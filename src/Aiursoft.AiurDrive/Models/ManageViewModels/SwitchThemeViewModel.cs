using System.ComponentModel.DataAnnotations;

namespace Aiursoft.AiurDrive.Models.ManageViewModels;

public class SwitchThemeViewModel
{
    [Required]
    public required string Theme { get; set; }
}
