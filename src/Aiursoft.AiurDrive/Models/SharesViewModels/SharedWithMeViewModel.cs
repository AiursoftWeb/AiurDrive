using Aiursoft.AiurDrive.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.SharesViewModels;

public class SharedWithMeViewModel : UiStackLayoutViewModel
{
    public SharedWithMeViewModel()
    {
        PageTitle = "Shared with Me";
    }
    public List<SiteShare> Shares { get; set; } = new();
    public Dictionary<string, string> RoleNames { get; set; } = new();
}