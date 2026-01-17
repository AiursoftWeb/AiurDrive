using Aiursoft.AiurDrive.Entities;

namespace Aiursoft.AiurDrive.Models.UsersViewModels;

public class UserWithRolesViewModel
{
    public required User User { get; set; }
    public required IList<string> Roles { get; set; }
}
