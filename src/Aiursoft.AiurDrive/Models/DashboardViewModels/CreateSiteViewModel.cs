using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;
using Aiursoft.CSTools.Attributes;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels;

public class CreateSiteViewModel : UiStackLayoutViewModel
{
    [Display(Name = "New Site Name")]
    [Required(ErrorMessage = "Site name is required.")]
    [ValidDomainName(ErrorMessage =  "Site name is invalid. Only letters, digits, and hyphens are allowed.")]
    public string? SiteName { get; set; }

    [Display(Name = "Allow anonymous viewing")]
    public bool AllowAnonymousView { get; set; }

    [Display(Name = "Description")]
    [MaxLength(300, ErrorMessage = "The {0} must be at max {1} characters long.")]
    public string? Description { get; set; }

    public CreateSiteViewModel()
    {
        PageTitle = "Create New Site";
    }
}
