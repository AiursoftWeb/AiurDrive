using Aiursoft.AiurDrive.Models;
using Aiursoft.AiurDrive.Configuration;
using Aiursoft.AiurDrive.Services;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

[Authorize]
public class VaultController(GlobalSettingsService settings) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "Vault",
        LinkOrder = 2)]
    public async Task<IActionResult> Index()
    {
        var model = new VaultViewModel
        {
            StorageLimitInGiB = await settings.GetIntSettingAsync(SettingsMap.MaxVaultStorageInGB)
        };
        return this.StackView(model);
    }
}
