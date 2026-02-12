using System.ComponentModel.DataAnnotations;
using Aiursoft.AiurDrive.Entities;

namespace Aiursoft.AiurDrive.Models.SharesViewModels;

public class AddShareViewModel
{
    [Display(Name = "Target User")]
    public string? TargetUserId { get; set; }

    [Display(Name = "Target Role")]
    public string? TargetRoleId { get; set; }
    
    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "Permission")]
    public SharePermission Permission { get; set; }
}
