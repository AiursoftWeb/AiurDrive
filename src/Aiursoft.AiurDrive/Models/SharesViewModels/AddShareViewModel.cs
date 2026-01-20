using System.ComponentModel.DataAnnotations;
using Aiursoft.AiurDrive.Entities;

namespace Aiursoft.AiurDrive.Models.SharesViewModels;

public class AddShareViewModel
{
    public string? TargetUserId { get; set; }
    public string? TargetRoleId { get; set; }
    
    [Required]
    public SharePermission Permission { get; set; }
}
