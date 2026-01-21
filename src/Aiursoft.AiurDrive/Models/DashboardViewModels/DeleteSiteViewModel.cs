using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels;

public class DeleteSiteViewModel : UiStackLayoutViewModel
{
    [Display(Name = "Site Name")]
    public string? SiteName { get; set; }

    [Display(Name = "Allow Anonymous Viewing")]
    public bool AllowAnonymousView { get; set; }

    [Display(Name = "Creation Time")]
    public DateTime CreationTime { get; set; }

    public DeleteSiteViewModel()
    {
        PageTitle = "Delete Site";
    }
}
