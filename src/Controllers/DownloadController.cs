using Aiursoft.AiurDrive.Services;
using Aiursoft.Canon;
using Aiursoft.WebTools.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

[Route("download")]
public class DownloadController(
    StorageService storage, 
    QRCodeService qrCodeService,
    HyperScaleService hyperScale) : ControllerBase
{
    [Route("{**FolderNames}")]
    public IActionResult Index(string folderNames)
    {
        if (folderNames.Contains(".."))
        {
            return BadRequest("Invalid path!");
        }
        
        var physicalPath = storage.GetFilePhysicalPath(folderNames);
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }
       
        return this.WebFile(physicalPath);
    }
    
    [Route("hyperscaled/{**FolderNames}")]
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
            return this.WebFile(possibleScaledPath);
        }
        
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }
        
        //var newPath = await hyperScale.HyperScaleImage(physicalPath, outputPath: storage.HyperScaleFolder);
        var newPath = await hyperScale.HyperScaleImage(physicalPath,
            outputPath: Path.Combine(storage.HyperScaleFolder, Path.GetDirectoryName(folderNames)!));
        return this.WebFile(newPath);
    }
    
    [Route("qrcode")]
    public IActionResult QrCode([FromQuery]string path)
    {
        var qrcodeXml = qrCodeService.ToQRCodeSvgXml(path);
        return Content(qrcodeXml, "image/svg+xml");
    }

    private string GetPossibleHyperScaledFilePath(string sourceFileFolderNames)
    {
        var relativePath = Path.GetDirectoryName(sourceFileFolderNames)!;
        var physicalPath = storage.GetFilePhysicalPath(sourceFileFolderNames);
        var physicalDirectory = System.IO.Path.GetDirectoryName(physicalPath)!;
        var physicalFileName = System.IO.Path.GetFileName(physicalPath);
        var physicalFileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(physicalFileName);
        var physicalExtension = System.IO.Path.GetExtension(physicalFileName);
        var scaledFileName =$"{physicalFileNameWithoutExtension}_SwinIR{physicalExtension}";
        var scaledPath = System.IO.Path.Combine(storage.HyperScaleFolder, relativePath, scaledFileName);
        return scaledPath;
    }
}