using Aiursoft.CSTools.Attributes;
using Aiursoft.CSTools.Tools;
using Aiursoft.AiurDrive.Configuration;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

/// <summary>
/// This controller is used to handle file operations like upload and download.
/// </summary>
[LimitPerMin]
public class FilesController(
    ImageProcessingService imageCompressor,
    ILogger<FilesController> logger,
    StorageService storage,
    GlobalSettingsService globalSettings) : ControllerBase
{
    [Route("upload/{subfolder}")]
    public async Task<IActionResult> Index(
        [FromRoute][ValidDomainName] string subfolder,
        [FromQuery] bool useVault = false)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        // Executing here will let the browser upload the file.
        try
        {
            _ = HttpContext.Request.Form.Files.FirstOrDefault()?.ContentType;
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }

        if (HttpContext.Request.Form.Files.Count < 1)
        {
            return BadRequest("No file uploaded!");
        }

        var file = HttpContext.Request.Form.Files.First();
        if (!new ValidFolderName().IsValid(file.FileName))
        {
            return BadRequest("Invalid file name!");
        }

        var storePath = Path.Combine(
            subfolder,
            DateTime.UtcNow.Year.ToString("D4"),
            DateTime.UtcNow.Month.ToString("D2"),
            DateTime.UtcNow.Day.ToString("D2"),
            file.FileName);
        
        // Save returns the logical path (e.g. avatar/2026/01/14/logo.png)
        var relativePath = await storage.Save(storePath, file, useVault);
        return Ok(new
        {
            Path = relativePath,
            InternetPath = storage.RelativePathToInternetUrl(relativePath, HttpContext, useVault)
        });
    }

    [Route("download/{**folderNames}")]
    public async Task<IActionResult> Download([FromRoute] string folderNames)
    {
        return await ProcessDownload(folderNames, isVault: false);
    }

    [Route("download-private/{**folderNames}")]
    public async Task<IActionResult> DownloadPrivate([FromRoute] string folderNames, [FromQuery] string token)
    {
        if (!storage.ValidateDownloadToken(folderNames, token))
        {
            return Unauthorized("Invalid or expired token.");
        }
        return await ProcessDownload(folderNames, isVault: true);
    }

    private async Task<IActionResult> ProcessDownload(string folderNames, bool isVault)
    {
        logger.LogInformation("File download requested for path: {FolderNames} (Vault: {IsVault})", folderNames, isVault);

        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        // 1. Check if resource exists in Workspace/Vault (using logical path to resolve)
        string physicalPath;
        try
        {
            physicalPath = storage.GetFilePhysicalPath(folderNames, isVault);
        }
        catch (ArgumentException)
        {
            return BadRequest("Attempted to access a restricted path.");
        }
        
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        // 2. Image Processing (using logical path)
        // If it is an image, we enforce privacy protection (ClearExif) or resizing
        if (physicalPath.IsStaticImage() && 
            await imageCompressor.IsValidImageAsync(physicalPath) && 
            await globalSettings.GetBoolSettingAsync(SettingsMap.AllowImagePreview))
        {
            logger.LogInformation("Processing image compression/clearing request for logical path: {Path}", folderNames);
            return await FileWithImageCompressor(folderNames, isVault);
        }

        // 3. Standard File Download (Non-image)
        logger.LogInformation("Processing raw file download request for path: {Path}", physicalPath);
        return this.WebFile(physicalPath);
    }

    private async Task<IActionResult> FileWithImageCompressor(string logicalPath, bool isVault)
    {
        var passedWidth = int.TryParse(Request.Query["w"], out var width);
        var passedSquare = bool.TryParse(Request.Query["square"], out var square);
        if (width > 0 && passedWidth)
        {
            width = SizeCalculator.Ceiling(width);
            logger.LogInformation("Compressing image '{Path}' to width: {Width}", logicalPath, width);
            if (square && passedSquare)
            {
                var compressedPath = await imageCompressor.CompressAsync(logicalPath, width, width, isVault);
                logger.LogInformation("Image compressed to square format: {CompressedPath}", compressedPath);
                return this.WebFile(compressedPath);
            }
            else
            {
                var compressedPath = await imageCompressor.CompressAsync(logicalPath, width, 0, isVault);
                logger.LogInformation("Image compressed to rectangular format: {CompressedPath}", compressedPath);
                return this.WebFile(compressedPath);
            }
        }
        else
        {
            logger.LogInformation("No valid width parameter provided for {Path}, width={Width}, passedWidth={PassedWidth}",
                logicalPath, width, passedWidth);
        }

        // If no width or invalid, just clear EXIF (Privacy by Default)
        logger.LogInformation("Clearing EXIF data for image: {Path}", logicalPath);
        var clearedPath = await imageCompressor.ClearExifAsync(logicalPath, isVault);
        return this.WebFile(clearedPath);
    }
}
