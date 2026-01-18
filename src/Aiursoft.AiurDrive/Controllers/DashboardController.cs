using Aiursoft.AiurDrive.Models.DashboardViewModels;
using Aiursoft.AiurDrive.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.AiurDrive.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.AiurDrive.Configuration;

namespace Aiursoft.AiurDrive.Controllers;

[LimitPerMin]
[Authorize]
public class DashboardController(
    TemplateDbContext dbContext,
    StorageService storage,
    GlobalSettingsService globalSettings,
    Microsoft.Extensions.Localization.IStringLocalizer<DashboardController> localizer) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "Index",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            
        if (user == null) return NotFound();

        var maxSites = await globalSettings.GetIntSettingAsync(SettingsMap.MaxSitesPerPerson);

        if (!user.Sites.Any() && maxSites > 0)
        {
            return RedirectToAction(nameof(CreateSite));
        }

        var model = new IndexViewModel
        {
            Sites = user.Sites,
            MaxSites = maxSites,
            CurrentSitesCount = user.Sites.Count()
        };
        return this.StackView(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateSite()
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return NotFound();

        var maxSites = await globalSettings.GetIntSettingAsync(SettingsMap.MaxSitesPerPerson);
        if (user.Sites.Count() >= maxSites)
        {
            return RedirectToAction(nameof(Index));
        }

        var model = new CreateSiteViewModel();
        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSite(CreateSiteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }

        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return NotFound();

        var maxSites = await globalSettings.GetIntSettingAsync(SettingsMap.MaxSitesPerPerson);
        if (user.Sites.Count() >= maxSites)
        {
            ModelState.AddModelError(string.Empty, localizer["You have reached the maximum number of sites ({0}).", maxSites]);
            return this.StackView(model);
        }

        var newSite = new Site
        {
            SiteName = model.SiteName!.ToLower(),
            AppUserId = user.Id,
            OpenToUpload = model.OpenToUpload
        };

        try
        {
            dbContext.Sites.Add(newSite);
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.SiteName), localizer["This site name is already taken."]);
            return this.StackView(model);
        }

        return RedirectToAction(nameof(Files), new { siteName = newSite.SiteName });
    }

    [Route("Dashboard/Files/{siteName}/{**path}")]
    public async Task<IActionResult> Files(string siteName, string? path)
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return NotFound();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        path ??= string.Empty;
        var logicalPath = Path.Combine(siteName, path);
        
        // Ensure the folder exists
        var physicalPath = storage.GetFilePhysicalPath(logicalPath, !site.OpenToUpload);
        if (!Directory.Exists(physicalPath))
        {
             Directory.CreateDirectory(physicalPath);
        }

        var directoryInfo = new DirectoryInfo(physicalPath);
        var files = directoryInfo.GetFiles().OrderByDescending(f => f.LastWriteTime).ToList();
        var folders = directoryInfo.GetDirectories().OrderBy(f => f.Name).Select(f => f.Name).ToList();

        var maxSpaceString = await globalSettings.GetSettingValueAsync(SettingsMap.MaxSiteStorageInGB);
        long.TryParse(maxSpaceString, out var maxSpaceGB);

        var model = new FileManagerViewModel
        {
            SiteName = siteName,
            OpenToUpload = site.OpenToUpload,
            Path = path.Replace("\\", "/"),
            Files = files,
            Folders = folders,
            PageTitle = "File Manager",
            UsedSpaceInBytes = storage.GetSiteSize(siteName),
            TotalSpaceInGB = maxSpaceGB,
            AllowImagePreview = await globalSettings.GetBoolSettingAsync(SettingsMap.AllowImagePreview)
        };
        return this.StackView(model);
    }



    [HttpPost]
    [Route("Dashboard/Delete/{siteName}/{**path}")]
    public async Task<IActionResult> Delete(string siteName, string? path)
    {
         var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return Unauthorized();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        if (string.IsNullOrWhiteSpace(path)) return BadRequest("Cannot delete root.");

        var logicalPath = Path.Combine(siteName, path);
        try 
        {
            var physicalPath = storage.GetFilePhysicalPath(logicalPath, !site.OpenToUpload);
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
            else if (Directory.Exists(physicalPath))
            {
                 Directory.Delete(physicalPath, true);
            }
            else
            {
                return NotFound();
            }
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        // Redirect back to the parent folder
        var parentPath = Path.GetDirectoryName(path)?.Replace("\\", "/");
        return RedirectToAction(nameof(Files), new { siteName, path = parentPath });
    }

    [HttpGet]
    [Route("Dashboard/DeleteSite/{siteName}")]
    public async Task<IActionResult> DeleteSite(string siteName)
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return NotFound();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        var model = new DeleteSiteViewModel
        {
            SiteName = site.SiteName,
            OpenToUpload = site.OpenToUpload,
            CreationTime = site.CreationTime
        };
        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Dashboard/DeleteSite/{siteName}")]
    [ActionName("DeleteSite")] // Ensures the form with asp-action="DeleteSite" posts here
    public async Task<IActionResult> DeleteSiteConfirmed(string siteName)
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return NotFound();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        // 1. Delete physical folder
        try 
        {
            storage.DeleteSiteFolder(site.SiteName);
        }
        catch (Exception e)
        {
            // Log error but continue to delete DB record? Or fail?
            // For now, let's assume if deletion fails we might want to stop, 
            // but usually we want to clear the DB record anyway if the folder is gone or partially gone.
            // Let's just proceed.
            Console.WriteLine($"Error deleting folder: {e.Message}");
        }

        // 2. Delete from DB
        dbContext.Sites.Remove(site);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("Dashboard/CreateFolder/{siteName}/{**path}")]
    public async Task<IActionResult> CreateFolder(string siteName, string? path, string newFolderName)
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return Unauthorized();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        if (string.IsNullOrWhiteSpace(newFolderName)) return BadRequest("Folder name cannot be empty.");
        if (newFolderName.Any(c => Path.GetInvalidFileNameChars().Contains(c))) return BadRequest("Invalid folder name.");

        path ??= string.Empty;
        var logicalPath = Path.Combine(siteName, path, newFolderName);

        try 
        {
            var physicalPath = storage.GetFilePhysicalPath(logicalPath, !site.OpenToUpload);
            if (Directory.Exists(physicalPath) || System.IO.File.Exists(physicalPath))
            {
                return BadRequest("File or folder already exists.");
            }
            Directory.CreateDirectory(physicalPath);
        }
        catch (Exception e)
        {
             return BadRequest(e.Message);
        }

        return RedirectToAction(nameof(Files), new { siteName, path });
    }

    [HttpPost]
    [Route("Dashboard/Rename/{siteName}/{**path}")]
    public async Task<IActionResult> Rename(string siteName, string path, string newName)
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return Unauthorized();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        if (string.IsNullOrWhiteSpace(path)) return BadRequest("Cannot rename root.");
        if (string.IsNullOrWhiteSpace(newName)) return BadRequest("New name cannot be empty.");
        if (newName.Any(c => Path.GetInvalidFileNameChars().Contains(c))) return BadRequest("Invalid file name.");

        var logicalPath = Path.Combine(siteName, path);
        
        try 
        {
            var oldPhysicalPath = storage.GetFilePhysicalPath(logicalPath, !site.OpenToUpload);
            var parentPhysicalPath = Directory.GetParent(oldPhysicalPath)?.FullName;
            if (parentPhysicalPath == null) return BadRequest("Cannot find parent directory.");

            // Security check: ensure new path is still valid within the storage root
            // We can do this by converting back to logical path or checking prefix, 
            // but calling GetFilePhysicalPath with the reconstructed logical path is safer.
            
            var parentLogicalPath = Path.GetDirectoryName(path); // relative to site
            var newLogicalPath = Path.Combine(siteName, parentLogicalPath ?? string.Empty, newName);
            
            // This throws if traversal detected
            var validatedNewPhysicalPath = storage.GetFilePhysicalPath(newLogicalPath, !site.OpenToUpload); 

            if (System.IO.File.Exists(validatedNewPhysicalPath) || Directory.Exists(validatedNewPhysicalPath))
            {
                return BadRequest("Target already exists.");
            }

            if (System.IO.File.Exists(oldPhysicalPath))
            {
                System.IO.File.Move(oldPhysicalPath, validatedNewPhysicalPath);
            }
            else if (Directory.Exists(oldPhysicalPath))
            {
                Directory.Move(oldPhysicalPath, validatedNewPhysicalPath);
            }
            else
            {
                return NotFound();
            }
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        var parentPath = Path.GetDirectoryName(path)?.Replace("\\", "/");
        return RedirectToAction(nameof(Files), new { siteName, path = parentPath });
    }

    [HttpPost]
    [Route("Dashboard/Move/{siteName}")]
    public async Task<IActionResult> Move(string siteName, string sourcePath, string? targetPath)
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return Unauthorized();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        if (string.IsNullOrWhiteSpace(sourcePath)) return BadRequest("Source path cannot be empty.");

        // Prevent moving into itself
        // Normalizing paths for comparison
        var normalizedSource = sourcePath.Replace("\\", "/").Trim('/');
        var normalizedTarget = (targetPath ?? string.Empty).Replace("\\", "/").Trim('/');
        
        // Check if target is same as source (useless move)
        if (string.Equals(Path.GetDirectoryName(normalizedSource)?.Replace("\\", "/"), normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
             return RedirectToAction(nameof(Files), new { siteName, path = normalizedTarget });
        }

        // Check for recursive move: Target starts with Source
        // e.g. Source: A, Target: A/B.  normalizedTarget (A/B) starts with normalizedSource (A)
        // Need to ensure we match directory boundaries, e.g. "Folder" starts with "Fold" is false positive.
        if (normalizedTarget.StartsWith(normalizedSource + "/", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(normalizedTarget, normalizedSource, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Cannot move a folder into itself or its subfolder.");
        }

        var fileName = Path.GetFileName(normalizedSource);
        var logicalDestPath = Path.Combine(siteName, normalizedTarget, fileName).Replace("\\", "/");
        var logicalSourcePath = Path.Combine(siteName, normalizedSource).Replace("\\", "/");

        try 
        {
            var physicalSource = storage.GetFilePhysicalPath(logicalSourcePath, !site.OpenToUpload);
            // We use GetFilePhysicalPath for destination too to ensure it resolves to a safe path inside the site
            // However, GetFilePhysicalPath usually checks for existence? 
            // Wait, looking at StorageService.GetFilePhysicalPath: it only checks "StartsWith root". It does NOT check File.Exists.
            // So it's safe to use for a non-existent target as long as it's valid.
            var physicalDest = storage.GetFilePhysicalPath(logicalDestPath, !site.OpenToUpload);

            if (System.IO.File.Exists(physicalDest) || Directory.Exists(physicalDest))
            {
                return BadRequest("Destination file or folder already exists.");
            }

            if (System.IO.File.Exists(physicalSource))
            {
                System.IO.File.Move(physicalSource, physicalDest);
            }
            else if (Directory.Exists(physicalSource))
            {
                Directory.Move(physicalSource, physicalDest);
            }
            else
            {
                return NotFound();
            }
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        return RedirectToAction(nameof(Files), new { siteName, path = targetPath });
    }
}
