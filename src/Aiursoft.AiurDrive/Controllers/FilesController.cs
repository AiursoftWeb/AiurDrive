using Aiursoft.CSTools.Attributes;
using Aiursoft.CSTools.Tools;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Aiursoft.AiurDrive.Entities;

namespace Aiursoft.AiurDrive.Controllers;

/// <summary>
/// This controller is used to handle file operations like upload and download.
/// </summary>
[LimitPerMin]
public class FilesController(
    ImageProcessingService imageCompressor,
    ILogger<FilesController> logger,
    StorageService storage,
    UserManager<User> userManager) : ControllerBase
{
    [Route("upload/{subfolder}")]
    public async Task<IActionResult> Index([FromRoute][ValidDomainName] string subfolder)
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
        var relativePath = await storage.Save(storePath, file);
        return Ok(new
        {
            Path = relativePath,
            InternetPath = storage.RelativePathToInternetUrl(relativePath, HttpContext)
        });
    }

    [Route("download/{**folderNames}")]
    public async Task<IActionResult> Download([FromRoute] string folderNames)
    {
        logger.LogInformation("File download requested for path: {FolderNames}", folderNames);

        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        string physicalPath;
        try
        {
            physicalPath = storage.GetFilePhysicalPath(folderNames);
        }
        catch (ArgumentException)
        {
            return BadRequest("Attempted to access a restricted path.");
        }
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        // SECURITY: Check if user has permission to access drive files
        var authResult = await ValidateDriveFileAccess(folderNames);
        if (authResult != null)
        {
            return authResult; // Unauthorized or Forbid
        }

        if (physicalPath.IsStaticImage() && await imageCompressor.IsValidImageAsync(physicalPath))
        {
            logger.LogInformation("Processing image compression request for path: {Path}", physicalPath);
            return await FileWithImageCompressor(physicalPath);
        }

        logger.LogInformation("Processing file download request for path: {Path}", physicalPath);
        return this.WebFile(physicalPath);
    }

    /// <summary>
    /// Validates that the current user has permission to access drive files.
    /// Returns null if authorized, or an ActionResult (Unauthorized/Forbid) if not.
    /// </summary>
    private async Task<IActionResult?> ValidateDriveFileAccess(string relativePath)
    {
        // Check if this is a drive file (drive/{userId}/...)
        if (!relativePath.StartsWith("drive/", StringComparison.OrdinalIgnoreCase))
        {
            // Not a drive file, allow access (e.g., Workspace files for avatars)
            return null;
        }

        // Extract userId from path: drive/{userId}/...
        var pathParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length < 2)
        {
            logger.LogWarning("Invalid drive path format: {Path}", relativePath);
            return BadRequest("Invalid drive path format.");
        }

        var requestedUserId = pathParts[1]; // drive/{userId}/...

        // Get current user
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            logger.LogWarning("Unauthenticated access attempt to drive file: {Path}", relativePath);
            return Unauthorized();
        }

        // Verify user can only access their own files
        if (currentUser.Id != requestedUserId)
        {
            logger.LogWarning("User {UserId} attempted to access files of user {RequestedUserId}", 
                currentUser.Id, requestedUserId);
            return Forbid();
        }

        // User is authorized
        return null;
    }

    private async Task<IActionResult> FileWithImageCompressor(string path)
    {
        var passedWidth = int.TryParse(Request.Query["w"], out var width);
        var passedSquare = bool.TryParse(Request.Query["square"], out var square);
        if (width > 0 && passedWidth)
        {
            width = SizeCalculator.Ceiling(width);
            logger.LogInformation("Compressing image '{Path}' to width: {Width}", path, width);
            if (square && passedSquare)
            {
                var compressedPath = await imageCompressor.CompressAsync(path, width, width);
                logger.LogInformation("Image compressed to square format: {CompressedPath}", compressedPath);
                return this.WebFile(compressedPath);
            }
            else
            {
                var compressedPath = await imageCompressor.CompressAsync(path, width, 0);
                logger.LogInformation("Image compressed to rectangular format: {CompressedPath}", compressedPath);
                return this.WebFile(compressedPath);
            }
        }
        else
        {
            logger.LogInformation("No valid width parameter provided for {Path}, width={Width}, passedWidth={PassedWidth}",
                path, width, passedWidth);
        }

        // If no width or invalid, just clear EXIF
        logger.LogInformation("Clearing EXIF data for image: {Path}", path);
        var clearedPath = await imageCompressor.ClearExifAsync(path);
        return this.WebFile(clearedPath);
    }
}
