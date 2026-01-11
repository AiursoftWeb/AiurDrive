using Aiursoft.AiurDrive.Authorization;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.PermissionsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Permissions";
    }

    public required List<PermissionWithRoleCount> Permissions { get; init; }
}

public class PermissionWithRoleCount
{
    public required PermissionDescriptor Permission { get; init; }
    public required int RoleCount { get; init; }
    public required int UserCount { get; init; }
}
