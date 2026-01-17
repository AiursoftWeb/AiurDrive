using Aiursoft.AiurDrive.Models.DashboardViewModels;
using Aiursoft.AiurDrive.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.AiurDrive.Entities;
using Microsoft.EntityFrameworkCore;
using Aiursoft.UiStack.Layout;
using Microsoft.AspNetCore.Authorization;
using Aiursoft.AiurDrive.Services.FileStorage;

namespace Aiursoft.AiurDrive.Controllers;

[LimitPerMin]
[Authorize]
public class DashboardController(
    TemplateDbContext dbContext,
    StorageService storage) : Controller
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

        if (!user.Sites.Any())
        {
            return RedirectToAction(nameof(CreateSite));
        }

        var model = new IndexViewModel
        {
            Sites = user.Sites
        };
        return this.StackView(model);
    }

    [HttpGet]
    public IActionResult CreateSite()
    {
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

        var newSite = new Site
        {
            SiteName = model.SiteName!.ToLower(),
            AppUserId = user.Id
        };

        try
        {
            dbContext.Sites.Add(newSite);
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.SiteName), "This site name is already taken.");
            return this.StackView(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [Route("Dashboard/Files/{siteName}/{**path}")]
    public async Task<IActionResult> Files(string siteName, string path)
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
        var physicalPath = storage.GetFilePhysicalPath(logicalPath);
        if (!Directory.Exists(physicalPath))
        {
             Directory.CreateDirectory(physicalPath);
        }

        var directoryInfo = new DirectoryInfo(physicalPath);
        var files = directoryInfo.GetFiles().OrderByDescending(f => f.LastWriteTime).ToList();
        var folders = directoryInfo.GetDirectories().OrderBy(f => f.Name).Select(f => f.Name).ToList();

        var model = new FileManagerViewModel
        {
            SiteName = siteName,
            Path = path.Replace("\\", "/"),
            Files = files,
            Folders = folders,
            PageTitle = "File Manager"
        };
        return this.StackView(model);
    }

    [HttpPost]
    [Route("Dashboard/Upload/{siteName}/{**path}")]
    public async Task<IActionResult> Upload(string siteName, string path)
    {
        var user = await dbContext.Users
            .Include(u => u.Sites)
            .SingleOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return Unauthorized();
        var site = user.Sites.FirstOrDefault(s => s.SiteName == siteName);
        if (site == null) return Unauthorized();

        path ??= string.Empty;
        
        // Validation
        if (HttpContext.Request.Form.Files.Count < 1)
        {
            return BadRequest(new { message = "No file uploaded!" });
        }

        var file = HttpContext.Request.Form.Files.First();
        var logicalPath = Path.Combine(siteName, path, file.FileName);

        // This will save the file using StorageService, handling conflicts
        var savedLogicalPath = await storage.Save(logicalPath, file);

        return Ok(new
        {
            Path = savedLogicalPath,
            InternetPath = storage.RelativePathToInternetUrl(savedLogicalPath, HttpContext)
        });
    }

    [HttpPost]
    [Route("Dashboard/Delete/{siteName}/{**path}")]
    public async Task<IActionResult> Delete(string siteName, string path)
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
            var physicalPath = storage.GetFilePhysicalPath(logicalPath);
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
}
