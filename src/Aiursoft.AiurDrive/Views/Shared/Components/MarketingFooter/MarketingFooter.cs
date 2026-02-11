using Microsoft.AspNetCore.Mvc;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Services.FileStorage;

namespace Aiursoft.AiurDrive.Views.Shared.Components.MarketingFooter;

public class MarketingFooter(
    GlobalSettingsService globalSettingsService,
    StorageService storageService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(MarketingFooterViewModel? model = null)
    {
        model ??= new MarketingFooterViewModel();
        model.BrandName = await globalSettingsService.GetSettingValueAsync("BrandName");
        model.BrandHomeUrl = await globalSettingsService.GetSettingValueAsync("BrandHomeUrl");
        model.Icp = await globalSettingsService.GetSettingValueAsync("Icp");
        
        var logoPath = await globalSettingsService.GetSettingValueAsync("ProjectLogo");
        if (!string.IsNullOrWhiteSpace(logoPath))
        {
            model.LogoUrl = storageService.RelativePathToInternetUrl(logoPath, HttpContext);
        }
        
        return View(model);
    }
}
