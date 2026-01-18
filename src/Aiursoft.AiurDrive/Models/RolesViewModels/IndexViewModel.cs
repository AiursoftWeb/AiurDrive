using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.RolesViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Roles";
    }

    public required List<IdentityRoleWithCount> Roles { get; init; }
}
