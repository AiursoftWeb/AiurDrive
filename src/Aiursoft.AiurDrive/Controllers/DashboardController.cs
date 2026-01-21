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
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.AiurDrive.Controllers;

[LimitPerMin]
[Authorize]
public class DashboardController(
    AiurDriveDbContext dbContext,
    StorageService storage,
    GlobalSettingsService globalSettings,
    UserManager<User> userManager,
    RoleManager<IdentityRole> roleManager,
    Microsoft.Extensions.Localization.IStringLocalizer<DashboardController> localizer) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "My Drive",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            
        if (user == null) return NotFound();
        var maxSites = await globalSettings.GetIntSettingAsync(SettingsMap.MaxSitesPerPerson);
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
            AllowAnonymousView = model.AllowAnonymousView,
            Description = model.Description
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

    [HttpGet]
    [Route("Dashboard/EditSite/{siteName}")]
    public async Task<IActionResult> EditSite(string siteName)
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return NotFound();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        var model = new EditSiteViewModel
        {
            SiteName = site.SiteName,
            Description = site.Description
        };
        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Dashboard/EditSite/{siteName}")]
    public async Task<IActionResult> EditSite(string siteName, EditSiteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }

        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return NotFound();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        site.Description = model.Description;
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasAccess(Site site, User user, SharePermission requiredPermission)
    {
        if (site.AppUserId == user.Id) return true;

        var userRoles = await userManager.GetRolesAsync(user);
        var userRoleIds = await roleManager.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        var share = await dbContext.SiteShares
            .Where(s => s.SiteId == site.Id)
            .Where(s => s.SharedWithUserId == user.Id || (s.SharedWithRoleId != null && userRoleIds.Contains(s.SharedWithRoleId)))
            .OrderByDescending(s => s.Permission)
            .FirstOrDefaultAsync();

        if (share == null) return false;

        if (requiredPermission == SharePermission.ReadOnly) return true;
        if (requiredPermission == SharePermission.Editable && share.Permission == SharePermission.Editable) return true;

        return false;
    }

    [Route("Dashboard/Files/{siteName}/{**path}")]
    public async Task<IActionResult> Files(string siteName, string? path)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var site = await dbContext.Sites.FirstOrDefaultAsync(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        if (!await HasAccess(site, user, SharePermission.ReadOnly))
        {
            return Forbid();
        }

        path ??= string.Empty;
        var logicalPath = Path.Combine(siteName, path);
        
        // Ensure the folder exists (all sites use Vault)
        var physicalPath = storage.GetFilePhysicalPath(logicalPath, isVault: true);
        if (!Directory.Exists(physicalPath))
        {
             Directory.CreateDirectory(physicalPath);
        }

        var directoryInfo = new DirectoryInfo(physicalPath);
        var files = directoryInfo.GetFiles().OrderByDescending(f => f.LastWriteTime).ToList();
        var folders = directoryInfo.GetDirectories().OrderBy(f => f.Name).Select(f => f.Name).ToList();

        var maxSpaceString = await globalSettings.GetSettingValueAsync(SettingsMap.MaxSiteStorageInGB);
        long.TryParse(maxSpaceString, out var maxSpaceGB);
        
        if (site.StorageSizeLimit.HasValue)
        {
            maxSpaceGB = site.StorageSizeLimit.Value;
        }

        var model = new FileManagerViewModel
        {
            SiteName = siteName,
            Path = path.Replace("\\", "/"),
            Files = files,
            Folders = folders,
            PageTitle = "File Manager",
            UsedSpaceInBytes = storage.GetSiteSize(siteName),
            TotalSpaceInGB = maxSpaceGB,
            AllowImagePreview = await globalSettings.GetBoolSettingAsync(SettingsMap.AllowImagePreview),
            IsOwner = site.AppUserId == user.Id,
            HasWriteAccess = await HasAccess(site, user, SharePermission.Editable)
        };
        return this.StackView(model);
    }

    [HttpPost]
    [Route("Dashboard/Delete/{siteName}/{**path}")]
    public async Task<IActionResult> Delete(string siteName, string? path)
    {
         var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var site = await dbContext.Sites.FirstOrDefaultAsync(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        if (!await HasAccess(site, user, SharePermission.Editable))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(path)) return BadRequest("Cannot delete root.");
        
        var normalizedPath = path.Replace("\\", "/");
        if (normalizedPath.Contains("..")) return BadRequest("Path traversal not allowed.");

        var logicalPath = Path.Combine(siteName, path);
        try 
        {
            var physicalPath = storage.GetFilePhysicalPath(logicalPath, isVault: true);
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

        // Only owner can delete site
        
        var model = new DeleteSiteViewModel
        {
            SiteName = site.SiteName,
            AllowAnonymousView = site.AllowAnonymousView,
            CreationTime = site.CreationTime
        };
        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Dashboard/DeleteSite/{siteName}")]
    [ActionName("DeleteSite")]
    public async Task<IActionResult> DeleteSiteConfirmed(string siteName)
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return NotFound();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        try 
        {
            storage.DeleteSiteFolder(site.SiteName);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error deleting folder: {e.Message}");
        }

        dbContext.Sites.Remove(site);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("Dashboard/CreateFolder/{siteName}/{**path}")]
    public async Task<IActionResult> CreateFolder(string siteName, string? path, string newFolderName)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var site = await dbContext.Sites.FirstOrDefaultAsync(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        if (!await HasAccess(site, user, SharePermission.Editable))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(newFolderName)) return BadRequest("Folder name cannot be empty.");
        if (newFolderName.Any(c => Path.GetInvalidFileNameChars().Contains(c))) return BadRequest("Invalid folder name.");

        path ??= string.Empty;
        var normalizedPath = path.Replace("\\", "/");
        if (normalizedPath.Contains("..")) return BadRequest("Path traversal not allowed.");
        
        var logicalPath = Path.Combine(siteName, path, newFolderName);

        try 
        {
            var physicalPath = storage.GetFilePhysicalPath(logicalPath, isVault: true);
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
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var site = await dbContext.Sites.FirstOrDefaultAsync(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        if (!await HasAccess(site, user, SharePermission.Editable))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(path)) return BadRequest("Cannot rename root.");
        if (string.IsNullOrWhiteSpace(newName)) return BadRequest("New name cannot be empty.");
        if (newName.Any(c => Path.GetInvalidFileNameChars().Contains(c))) return BadRequest("Invalid file name.");
        
        var normalizedPath = path.Replace("\\", "/");
        if (normalizedPath.Contains("..")) return BadRequest("Path traversal not allowed.");

        var logicalPath = Path.Combine(siteName, path);
        
        try 
        {
            var oldPhysicalPath = storage.GetFilePhysicalPath(logicalPath, isVault: true);
            var parentPhysicalPath = Directory.GetParent(oldPhysicalPath)?.FullName;
            if (parentPhysicalPath == null) return BadRequest("Cannot find parent directory.");
            
            var parentLogicalPath = Path.GetDirectoryName(path);
            var newLogicalPath = Path.Combine(siteName, parentLogicalPath ?? string.Empty, newName);
            
            var validatedNewPhysicalPath = storage.GetFilePhysicalPath(newLogicalPath, isVault: true); 

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
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var site = await dbContext.Sites.FirstOrDefaultAsync(s => s.SiteName == siteName);
        if (site == null) return NotFound();

        if (!await HasAccess(site, user, SharePermission.Editable))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(sourcePath)) return BadRequest("Source path cannot be empty.");

        var normalizedSource = sourcePath.Replace("\\", "/").Trim('/');
        var normalizedTarget = (targetPath ?? string.Empty).Replace("\\", "/").Trim('/');

        if (normalizedSource.Contains("..") || normalizedTarget.Contains(".."))
        {
            return BadRequest("Path traversal not allowed.");
        }
        
        if (string.Equals(Path.GetDirectoryName(normalizedSource)?.Replace("\\", "/"), normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
             return RedirectToAction(nameof(Files), new { siteName, path = normalizedTarget });
        }

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
            var physicalSource = storage.GetFilePhysicalPath(logicalSourcePath, isVault: true);
            var physicalDest = storage.GetFilePhysicalPath(logicalDestPath, isVault: true);

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
