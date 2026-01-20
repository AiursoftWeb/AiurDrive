using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Models.SharesViewModels;
using Aiursoft.AiurDrive.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Controllers;

[Authorize]
[LimitPerMin]
public class SharesController(
    UserManager<User> userManager,
    AiurDriveDbContext context,
    RoleManager<IdentityRole> roleManager,
    ILogger<SharesController> logger) : Controller
{
    [HttpGet]
    [Route("Dashboard/ManageShares/{siteName}")]
    public async Task<IActionResult> Manage(string siteName)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var site = await context.Sites
            .Include(s => s.SiteShares)
                .ThenInclude(ss => ss.SharedWithUser)
            .FirstOrDefaultAsync(s => s.SiteName == siteName);

        if (site == null) return NotFound();

        // Only owner can manage shares
        if (site.AppUserId != user.Id)
        {
            return Forbid();
        }

        var allRoles = await roleManager.Roles.ToListAsync();
        
        var publicLink = $"{Request.Scheme}://{Request.Host}/{siteName}";

        var model = new ManageSharesViewModel
        {
            SiteName = site.SiteName,
            IsPublic = site.OpenToUpload, 
            PublicLink = publicLink,
            ExistingShares = site.SiteShares.ToList(),
            AvailableRoles = allRoles
        };

        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Dashboard/AddShare/{siteName}")]
    public async Task<IActionResult> AddShare(string siteName, AddShareViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var site = await context.Sites
            .FirstOrDefaultAsync(s => s.SiteName == siteName);

        if (site == null) return NotFound();

        if (site.AppUserId != user.Id)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var targetUserId = string.IsNullOrWhiteSpace(model.TargetUserId) ? null : model.TargetUserId;
        var targetRoleId = string.IsNullOrWhiteSpace(model.TargetRoleId) ? null : model.TargetRoleId;

        if (targetUserId == null && targetRoleId == null)
        {
            return RedirectToAction(nameof(Manage), new { siteName, error = "invalid" });
        }

        var exists = await context.SiteShares
            .AnyAsync(s => s.SiteId == site.Id &&
                           ((targetUserId != null && s.SharedWithUserId == targetUserId) ||
                            (targetRoleId != null && s.SharedWithRoleId == targetRoleId)));

        if (exists)
        {
            return RedirectToAction(nameof(Manage), new { siteName, error = "duplicate" });
        }

        var share = new SiteShare
        {
            SiteId = site.Id,
            SharedWithUserId = targetUserId,
            SharedWithRoleId = targetRoleId,
            Permission = model.Permission
        };

        context.SiteShares.Add(share);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Site '{SiteName}' was shared by user '{UserId}'.", siteName, user.Id);

        return RedirectToAction(nameof(Manage), new { siteName });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Dashboard/RemoveShare/{id}")]
    public async Task<IActionResult> RemoveShare(int id)
    {
         var user = await userManager.GetUserAsync(User);
         if (user == null) return Unauthorized();

         var share = await context.SiteShares
             .Include(s => s.Site)
             .FirstOrDefaultAsync(s => s.Id == id);

         if (share == null) return NotFound();

         if (share.Site!.AppUserId != user.Id)
         {
             return Forbid();
         }

         context.SiteShares.Remove(share);
         await context.SaveChangesAsync();

         return RedirectToAction(nameof(Manage), new { siteName = share.Site.SiteName });
    }

    [HttpGet]
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "share-2",
        CascadedLinksOrder = 3,
        LinkText = "Shared with me",
        LinkOrder = 3)]
    public async Task<IActionResult> SharedWithMe()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var userRoles = await userManager.GetRolesAsync(user);
        var userRoleIds = await roleManager.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        var shares = await context.SiteShares
            .Include(s => s.Site)
                .ThenInclude(site => site!.AppUser) // Owner info
            .Where(s => s.SharedWithUserId == user.Id || (s.SharedWithRoleId != null && userRoleIds.Contains(s.SharedWithRoleId)))
            .OrderByDescending(s => s.CreationTime)
            .ToListAsync();

        var roleIds = shares.Where(s => s.SharedWithRoleId != null).Select(s => s.SharedWithRoleId).Distinct().ToList();
        var roles = await roleManager.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name!);

        var model = new SharedWithMeViewModel
        {
            Shares = shares,
            RoleNames = roles
        };

        return this.StackView(model);
    }
}
