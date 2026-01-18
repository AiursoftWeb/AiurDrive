using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Models.SharedViewModels;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.AiurDrive.Configuration;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Controllers;

[LimitPerMin]
public class SharedViewController(
    TemplateDbContext dbContext,
    StorageService storage,
    GlobalSettingsService globalSettings) : Controller
{
    [Route("SharedView/Index/{siteName}/{**path}")]
    public async Task<IActionResult> Index(string siteName, string? path)
    {
        var site = await dbContext.Sites
            .Include(s => s.AppUser)
            .SingleOrDefaultAsync(s => s.SiteName == siteName);

        if (site == null) return NotFound();
        if (!site.OpenToUpload) return Unauthorized();

        path ??= string.Empty;
        var logicalPath = Path.Combine(siteName, path);
        
        // Ensure the folder exists (for consistency, though shared view shouldn't really create them)
        var physicalPath = storage.GetFilePhysicalPath(logicalPath, isVault: false);
        if (!Directory.Exists(physicalPath))
        {
             Directory.CreateDirectory(physicalPath);
        }

        var directoryInfo = new DirectoryInfo(physicalPath);
        var files = directoryInfo.GetFiles().OrderByDescending(f => f.LastWriteTime).ToList();
        var folders = directoryInfo.GetDirectories().OrderBy(f => f.Name).Select(f => f.Name).ToList();

        var model = new SharedViewModel
        {
            Site = site,
            Path = path.Replace("\\", "/"),
            Files = files,
            Folders = folders,
            PageTitle = $"Shared - {site.SiteName}",
            AllowImagePreview = await globalSettings.GetBoolSettingAsync(SettingsMap.AllowImagePreview)
        };
        return this.StackView(model);
    }
}
