using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels;

public class EditSiteViewModel : UiStackLayoutViewModel
{
    public required string SiteName { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }

    public EditSiteViewModel()
    {
        PageTitle = "Edit Site Settings";
    }
}
