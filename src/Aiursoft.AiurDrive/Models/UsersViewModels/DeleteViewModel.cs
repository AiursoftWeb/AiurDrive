using Aiursoft.AiurDrive.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.UsersViewModels;

public class DeleteViewModel : UiStackLayoutViewModel
{
    public DeleteViewModel()
    {
        PageTitle = "Delete User";
    }

    public required User User { get; set; }
}
