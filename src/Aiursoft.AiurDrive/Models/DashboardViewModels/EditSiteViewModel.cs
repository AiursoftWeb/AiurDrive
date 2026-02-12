using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels;

public class EditSiteViewModel : UiStackLayoutViewModel
{
    [Display(Name = "Site Name")]
    [Required(ErrorMessage = "The {0} is required.")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Only lowercase letters, numbers and hyphens are allowed.")]
    public required string SiteName { get; set; }

    [Display(Name = "Description")]
    [MaxLength(300, ErrorMessage = "The {0} must be at max {1} characters long.")]
    public string? Description { get; set; }

    [Display(Name = "Allow Anonymous View")]
    public bool AllowAnonymousView { get; set; }

    public EditSiteViewModel()
    {
        PageTitle = "Edit Site Settings";
    }
}
