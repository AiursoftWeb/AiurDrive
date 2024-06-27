using Aiursoft.AiurDrive.Services;
using Aiursoft.CSTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

[Route("upload")]
public class UploadController(StorageService storage) : ControllerBase
{
    public async Task<IActionResult> Index()
    {
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
            DateTime.UtcNow.Year.ToString("D4"), 
            DateTime.UtcNow.Month.ToString("D2"), 
            DateTime.UtcNow.Day.ToString("D2"),
            file.FileName);
        var relativePath = await storage.Save(storePath, file);
        var urlPath = Uri.EscapeDataString(relativePath)
            .Replace("%5C", "/")
            .Replace("%5c", "/")
            .Replace("%2F", "/")
            .Replace("%2f", "/")
            .TrimStart('/');

        return Ok(new
        {
            InternetPath = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/download/{urlPath}",
        });
    }    
}