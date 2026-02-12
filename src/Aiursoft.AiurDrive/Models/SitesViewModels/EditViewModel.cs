using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.SitesViewModels;

public class EditViewModel : UiStackLayoutViewModel
{
    public EditViewModel()
    {
        PageTitle = "Edit Site Storage Limit";
    }

    [Display(Name = "Site ID")]
    public int SiteId { get; set; }
    
    [Display(Name = "Site Name")]
    public string? SiteName { get; set; }

    [Display(Name = "Storage Size Limit (GB)")]
    public long? StorageSizeLimit { get; set; }
}
