using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.SitesViewModels;

public class EditViewModel : UiStackLayoutViewModel
{
    public EditViewModel()
    {
        PageTitle = "Edit Site Storage Limit";
    }

    public int SiteId { get; set; }
    
    public string? SiteName { get; set; }

    [Display(Name = "Storage Size Limit (GB)")]
    public long? StorageSizeLimit { get; set; }
}
