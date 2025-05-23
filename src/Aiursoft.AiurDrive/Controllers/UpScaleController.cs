using Aiursoft.AiurDrive.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Aiursoft.AiurDrive.Controllers;

[Route("upscale")]
public class UpScaleController(
    IOptions<AiFeaturesSettings> aiFeatures,
    StorageService storage,
    UpScaleService upScale) : ControllerBase
{
    [LimitPerMin(15)]
    [Route("{**FolderNames}")]
    public async Task<IActionResult> TriggerUpScale(string folderNames)
    {
        if (!aiFeatures.Value.HyperScaling)
        {
            return BadRequest("Hyper scaling is not enabled on this server.");
        }

        if (folderNames.Contains(".."))
        {
            return BadRequest("Invalid path!");
        }

        var physicalPath = storage.GetFilePhysicalPath(folderNames);

        // In case we have already up scaled this photo, directly return the path.
        var possibleScaledPath = GetPossibleUpScaledFilePath(folderNames);
        if (System.IO.File.Exists(possibleScaledPath))
        {
            var cachedUriPath = storage.RelativePathToUriPath(storage.AbsolutePathToRelativePath(possibleScaledPath));
            return Ok($"{Request.Scheme}://{Request.Host}/download/{cachedUriPath}");
        }

        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        if (!await upScale.IsSupportedImageFileAsync(physicalPath))
        {
            return BadRequest("Unsupported file type. Please try a smaller photo.");
        }

        var upScaledPath = await upScale.UpScaleImage(physicalPath,
            outputPath: Path.Combine(storage.WorkspaceFolder, "UpScaled", Path.GetDirectoryName(folderNames)!));
        var uriPath = storage.RelativePathToUriPath(storage.AbsolutePathToRelativePath(upScaledPath));
        return Ok($"{Request.Scheme}://{Request.Host}/download/{uriPath}");
    }

    private string GetPossibleUpScaledFilePath(string sourceFileFolderNames)
    {
        var relativePath = Path.GetDirectoryName(sourceFileFolderNames)!;
        var physicalPath = storage.GetFilePhysicalPath(sourceFileFolderNames);
        var physicalFileName = Path.GetFileName(physicalPath);
        var physicalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(physicalFileName);
        var physicalExtension = ".jpg"; // All up scaled images are jpg.
        var scaledFileName =$"{physicalFileNameWithoutExtension}_SwinIR{physicalExtension}";
        var scaledPath = Path.Combine(storage.WorkspaceFolder, "UpScaled", relativePath, scaledFileName);
        return scaledPath;
    }
}
