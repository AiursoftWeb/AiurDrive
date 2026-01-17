using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels;

public class SettingsViewModel : UiStackLayoutViewModel
{
    [Display(Name = "Site Name")]
    public string? SiteName { get; set; }

    [Display(Name = "Storage Type")]
    public bool OpenToUpload { get; set; }

    public SettingsViewModel()
    {
        PageTitle = "Site Settings";
    }
}
