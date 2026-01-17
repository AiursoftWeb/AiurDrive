using Aiursoft.AiurDrive.Models.DashboardViewModels;
using Aiursoft.AiurDrive.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.AiurDrive.Entities;
using Microsoft.EntityFrameworkCore;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Controllers;

[LimitPerMin]
public class DashboardController(TemplateDbContext dbContext) : Controller
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
}
