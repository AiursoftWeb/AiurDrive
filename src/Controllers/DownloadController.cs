using Aiursoft.AiurDrive.Services;
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
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }
        var newPath = await hyperScale.HyperScaleImage(physicalPath, outputPath: storage.HyperScaleFolder);
        return this.WebFile(newPath);
    }
    
    [Route("qrcode")]
    public IActionResult QrCode([FromQuery]string path)
    {
        var qrcodeXml = qrCodeService.ToQRCodeSvgXml(path);
        return Content(qrcodeXml, "image/svg+xml");
    }
}