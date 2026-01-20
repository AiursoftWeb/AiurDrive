using Aiursoft.AiurDrive.Authorization;
using Aiursoft.AiurDrive.Configuration;
using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Models.SitesViewModels;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Controllers;

[Authorize(Policy = AppPermissionNames.CanOverrideSiteStorage)]
[LimitPerMin]
public class SitesController(
    AiurDriveDbContext dbContext,
    StorageService storageService,
    GlobalSettingsService globalSettings) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "System",
        CascadedLinksIcon = "cog",
        CascadedLinksOrder = 9999,
        LinkText = "Sites Management",
        LinkOrder = 2)]
    public async Task<IActionResult> Index(int page = 1)
    {
        if (page < 1) page = 1;
        var pageSize = 20;
        var sites = await dbContext.Sites
            .Include(s => s.AppUser)
            .OrderByDescending(s => s.CreationTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        var totalCount = await dbContext.Sites.CountAsync();
        
        var maxSpaceString = await globalSettings.GetSettingValueAsync(SettingsMap.MaxSiteStorageInGB);
        long.TryParse(maxSpaceString, out var globalMaxSpaceGB);

        var viewModels = new List<SiteStorageViewModel>();
        foreach(var site in sites)
        {
            var usedSize = storageService.GetSiteSize(site.SiteName);
            var limit = site.StorageSizeLimit ?? globalMaxSpaceGB;
            viewModels.Add(new SiteStorageViewModel
            {
                Site = site,
                UsedSizeInBytes = usedSize,
                LimitInGB = limit,
                IsOverridden = site.StorageSizeLimit.HasValue
            });
        }

        var model = new IndexViewModel
        {
            Sites = viewModels,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
        
        return this.StackView(model);
    }

    [HttpGet]
    [Route("Sites/Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var site = await dbContext.Sites.FindAsync(id);
        if (site == null) return NotFound();

        var model = new EditViewModel
        {
            SiteId = site.Id,
            SiteName = site.SiteName,
            StorageSizeLimit = site.StorageSizeLimit
        };
        return this.StackView(model);
    }

    [HttpPost]
    [Route("Sites/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditViewModel model)
    {
        if (!ModelState.IsValid) return this.StackView(model);
        
        var site = await dbContext.Sites.FindAsync(id);
        if (site == null) return NotFound();

        site.StorageSizeLimit = model.StorageSizeLimit;
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
