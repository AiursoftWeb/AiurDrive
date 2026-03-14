using Aiursoft.AiurDrive.Models;
using Aiursoft.AiurDrive.Services;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

[Authorize]
public class VaultController : Controller
{
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "Vault",
        LinkOrder = 2)]
    public IActionResult Index()
    {
        var model = new VaultViewModel
        {
            PageTitle = "Zero-Trust Vault"
        };
        return this.StackView(model);
    }
}
