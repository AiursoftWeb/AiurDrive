using Aiursoft.AiurDrive.Entities;
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
    FeatureFoldersProvider folders) : Controller
{
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
        public string VaultSaltBase64 { get; init; } = string.Empty;
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
    [DisableRequestSizeLimit]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> Upload(IFormFile? file, IFormFile? meta)
    {
        if (meta == null || meta.Length == 0)
        {
            return BadRequest(localizer["No metadata provided."]);
        }

        var objectsPath = GetUserVaultObjectsPath();
        var uuid = Path.GetFileNameWithoutExtension(meta.FileName);
        if (string.IsNullOrWhiteSpace(uuid) || uuid.Any(c => !char.IsLetterOrDigit(c)))
        {
            return BadRequest(localizer["Invalid object name."]);
        }

        // Always save meta
        var metaPath = Path.Combine(objectsPath, uuid + ".meta");
        await using (var metaStream = new FileStream(metaPath, FileMode.Create))
        {
            await meta.CopyToAsync(metaStream);
        }

        // Save encrypted content if present (it's a file, not just a folder)
        if (file != null && file.Length > 0)
        {
            var filePath = Path.Combine(objectsPath, uuid + ".enc");
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        return Ok();
    }

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
    public IActionResult Delete(string uuid)
    {
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
