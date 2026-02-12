using System.ComponentModel.DataAnnotations;

namespace Aiursoft.AiurDrive.Models.ManageViewModels;

public class SwitchThemeViewModel
{
    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "Theme")]
    public required string Theme { get; set; }
}
