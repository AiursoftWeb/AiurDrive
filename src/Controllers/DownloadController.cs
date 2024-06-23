using Aiursoft.AiurDrive.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

[Route("download")]
public class DownloadController(StorageService storage) : Controller
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
}