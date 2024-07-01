using Aiursoft.AiurDrive.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

[Route("hyperscale")]
public class HyperScaleController(
    StorageService storage, 
    HyperScaleService hyperScale) : ControllerBase
{
        
    [LimitPerMin(5)]
    [Route("{**FolderNames}")]
    public async Task<IActionResult> DownloadHyperScaled(string folderNames)
    {
        if (folderNames.Contains(".."))
        {
            return BadRequest("Invalid path!");
        }
        
        var physicalPath = storage.GetFilePhysicalPath(folderNames);
        var possibleScaledPath = GetPossibleHyperScaledFilePath(folderNames);
        if (System.IO.File.Exists(possibleScaledPath))
        {
            var cachedUriPath = storage.RelativePathToUriPath(storage.AbsolutePathToRelativePath(possibleScaledPath));
            return Ok($"{Request.Scheme}://{Request.Host}/download/{cachedUriPath}");
        }
        
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }
        
        if (!await hyperScale.IsSupportedImageFileAsync(physicalPath))
        {
            return BadRequest("Unsupported file type!");
        }
        
        var hyperScaledPath = await hyperScale.HyperScaleImage(physicalPath,
            outputPath: Path.Combine(storage.WorkspaceFolder, "HyperScaled", Path.GetDirectoryName(folderNames)!));
        var uriPath = storage.RelativePathToUriPath(storage.AbsolutePathToRelativePath(hyperScaledPath));
        return Ok($"{Request.Scheme}://{Request.Host}/download/{uriPath}");
    }
    
    private string GetPossibleHyperScaledFilePath(string sourceFileFolderNames)
    {
        var relativePath = Path.GetDirectoryName(sourceFileFolderNames)!;
        var physicalPath = storage.GetFilePhysicalPath(sourceFileFolderNames);
        var physicalFileName = Path.GetFileName(physicalPath);
        var physicalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(physicalFileName);
        var physicalExtension = ".jpg"; // All hyperscaled images are jpg.
        var scaledFileName =$"{physicalFileNameWithoutExtension}_SwinIR{physicalExtension}";
        var scaledPath = Path.Combine(storage.WorkspaceFolder, "HyperScaled", relativePath, scaledFileName);
        return scaledPath;
    }
}