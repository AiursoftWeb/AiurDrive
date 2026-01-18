using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Models.ExploreViewModels;
using Aiursoft.AiurDrive.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Controllers;

[LimitPerMin]
public class ExploreController(TemplateDbContext dbContext) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Explore",
        CascadedLinksIcon = "compass",
        CascadedLinksOrder = 2,
        LinkText = "Public Sites",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var publicSites = await dbContext.Sites
            .Include(s => s.AppUser)
            .Where(s => s.OpenToUpload)
            .OrderByDescending(s => s.CreationTime)
            .ToListAsync();

        var model = new IndexViewModel
        {
            PublicSites = publicSites,
            PageTitle = "Explore Public Sites"
        };
        return this.StackView(model);
    }
}
