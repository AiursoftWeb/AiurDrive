using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Configuration;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace Aiursoft.AiurDrive.Controllers;

[LimitPerMin]
[Authorize]
[Route("VaultApi")]
public class VaultApiController(
    UserManager<User> userManager,
    IStringLocalizer<VaultApiController> localizer,
    FeatureFoldersProvider folders,
    DriveStorageQuota quota,
    GlobalSettingsService settings,
    ILogger<VaultApiController> logger) : Controller
{
    private const long MaxEncryptedFileBytes = 2L * 1024 * 1024 * 1024;
    private const long MaxMetadataBytes = 1024 * 1024;
    private const long MaxUploadRequestBytes = MaxEncryptedFileBytes + MaxMetadataBytes + 64 * 1024;

    private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) 
        ?? throw new UnauthorizedAccessException();

    private string GetUserVaultObjectsPath()
    {
        var userId = GetCurrentUserId();
        var path = Path.Combine(folders.GetVaultFolder(), "_ZeroTrust", userId, "objects");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    [HttpGet]
    [Route("GetConfig")]
    public async Task<IActionResult> GetConfig()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrEmpty(user.VaultSaltBase64))
        {
            return NotFound();
        }

        return Json(new
        {
            user.VaultSaltBase64,
            user.VerifierNonceBase64,
            user.VerifierTagBase64,
            user.VerifierCipherBase64
        });
    }

    public class VaultConfig
    {
        public string VaultSaltBase64 { get; set; } = string.Empty;
        public string VerifierNonceBase64 { get; set; } = string.Empty;
        public string VerifierTagBase64 { get; set; } = string.Empty;
        public string VerifierCipherBase64 { get; set; } = string.Empty;
    }

    [HttpPost]
    [Route("Init")]
    public async Task<IActionResult> Init([FromBody] VaultConfig config)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (!string.IsNullOrEmpty(user.VaultSaltBase64))
        {
            return BadRequest(localizer["Vault already initialized."]);
        }

        user.VaultSaltBase64 = config.VaultSaltBase64;
        user.VerifierNonceBase64 = config.VerifierNonceBase64;
        user.VerifierTagBase64 = config.VerifierTagBase64;
        user.VerifierCipherBase64 = config.VerifierCipherBase64;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.First().Description);
        }

        return Ok();
    }

    [HttpGet]
    [Route("List")]
    public IActionResult List()
    {
        var objectsPath = GetUserVaultObjectsPath();
        var directoryInfo = new DirectoryInfo(objectsPath);
        // Use .meta files as the source of truth
        var metaFiles = directoryInfo.GetFiles("*.meta");
        
        var result = metaFiles.Select(f =>
        {
            var uuid = Path.GetFileNameWithoutExtension(f.Name);
            var encPath = Path.Combine(objectsPath, uuid + ".enc");
            long size = 0;
            if (System.IO.File.Exists(encPath))
            {
                size = new FileInfo(encPath).Length;
            }
            
            var metaContent = System.IO.File.ReadAllBytes(f.FullName);
            return new
            {
                Uuid = uuid,
                Size = size,
                LastWriteTime = f.LastWriteTimeUtc,
                MetaBase64 = Convert.ToBase64String(metaContent)
            };
        }).ToList();

        return Json(result);
    }

    [HttpPost]
    [Route("Upload")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(ValueLengthLimit = 16 * 1024, MultipartBodyLengthLimit = MaxUploadRequestBytes)]
    public async Task<IActionResult> Upload(IFormFile? file, IFormFile? meta)
    {
        if (meta == null || meta.Length == 0)
        {
            return BadRequest(localizer["No metadata provided."]);
        }

        if (meta.Length > MaxMetadataBytes || file?.Length > MaxEncryptedFileBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var objectsPath = GetUserVaultObjectsPath();
        var uuid = Path.GetFileNameWithoutExtension(meta.FileName);
        if (string.IsNullOrWhiteSpace(uuid) || uuid.Any(c => !char.IsLetterOrDigit(c)))
        {
            return BadRequest(localizer["Invalid object name."]);
        }

        await using var lease = await quota.LockAsync("vault:" + GetCurrentUserId(), HttpContext.RequestAborted);
        Directory.CreateDirectory(objectsPath);
        var metaPath = Path.Combine(objectsPath, uuid + ".meta");
        var filePath = Path.Combine(objectsPath, uuid + ".enc");
        var hasContent = file is { Length: > 0 };
        var delta = meta.Length - ExistingSize(metaPath) +
                    (hasContent ? file!.Length - ExistingSize(filePath) : 0);
        var limit = await settings.GetIntSettingAsync(SettingsMap.MaxVaultStorageInGB);
        try
        {
            quota.Check(checked(limit * DriveStorageQuota.GiB), delta, objectsPath);
        }
        catch (StorageQuotaExceededException)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, localizer["Storage quota exceeded."].Value);
        }

        // Stage both parts before replacing either existing part. Interrupted writes leave
        // the previous object intact; failed commits restore their original files.
        var staging = Path.Combine(objectsPath, ".uploads", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        var replacements = new List<(string Target, string Staged, string Backup, bool Existed)>();
        var preserveStaging = false;
        try
        {
            var parts = new List<(IFormFile Part, string Target)> { (meta, metaPath) };
            if (hasContent) parts.Add((file!, filePath));
            foreach (var (part, target) in parts)
            {
                var staged = Path.Combine(staging, Path.GetFileName(target));
                await using var output = new FileStream(staged, FileMode.CreateNew);
                await part.CopyToAsync(output, HttpContext.RequestAborted);
                replacements.Add((target, staged, staged + ".old", System.IO.File.Exists(target)));
            }
            try
            {
                foreach (var item in replacements)
                {
                    if (item.Existed) System.IO.File.Move(item.Target, item.Backup);
                    System.IO.File.Move(item.Staged, item.Target);
                }
            }
            catch
            {
                try
                {
                    foreach (var item in replacements.AsEnumerable().Reverse())
                    {
                        if (System.IO.File.Exists(item.Backup))
                        {
                            System.IO.File.Move(item.Backup, item.Target, overwrite: true);
                        }
                        else if (!item.Existed && !System.IO.File.Exists(item.Staged))
                        {
                            System.IO.File.Delete(item.Target);
                        }
                    }
                }
                catch
                {
                    preserveStaging = true;
                    throw;
                }
                throw;
            }
        }
        finally
        {
            try { if (!preserveStaging) Directory.Delete(staging, recursive: true); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Remaining staging files are still included in the next quota calculation.
                logger.LogWarning(exception, "Could not remove an encrypted upload staging directory.");
            }
        }

        return Ok();
    }

    private static long ExistingSize(string path) => System.IO.File.Exists(path) ? new FileInfo(path).Length : 0;

    [HttpGet]
    [Route("Download/{uuid}")]
    public IActionResult Download(string uuid)
    {
        var objectsPath = GetUserVaultObjectsPath();
        if (uuid.Any(c => !char.IsLetterOrDigit(c))) return BadRequest();

        var filePath = Path.Combine(objectsPath, uuid + ".enc");
        if (!System.IO.File.Exists(filePath)) return NotFound();

        return PhysicalFile(filePath, "application/octet-stream");
    }

    [HttpDelete]
    [Route("Delete/{uuid}")]
    public async Task<IActionResult> Delete(string uuid)
    {
        await using var lease = await quota.LockAsync("vault:" + GetCurrentUserId(), HttpContext.RequestAborted);
        var objectsPath = GetUserVaultObjectsPath();
        if (uuid.Any(c => !char.IsLetterOrDigit(c))) return BadRequest();

        var filePath = Path.Combine(objectsPath, uuid + ".enc");
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
        
        var metaPath = Path.Combine(objectsPath, uuid + ".meta");
        if (System.IO.File.Exists(metaPath))
        {
            System.IO.File.Delete(metaPath);
        }

        return Ok();
    }

    [HttpPost]
    [Route("Reset")]
    public async Task<IActionResult> Reset()
    {
        await using var lease = await quota.LockAsync("vault:" + GetCurrentUserId(), HttpContext.RequestAborted);
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // 1. Clear database config
        user.VaultSaltBase64 = null;
        user.VerifierNonceBase64 = null;
        user.VerifierTagBase64 = null;
        user.VerifierCipherBase64 = null;
        await userManager.UpdateAsync(user);

        // 2. Physical delete
        var userId = GetCurrentUserId();
        var path = Path.Combine(folders.GetVaultFolder(), "_ZeroTrust", userId);
        FolderDeleter.DeleteByForce(path);

        return Ok();
    }
}
