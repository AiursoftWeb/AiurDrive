using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Dashboard";
    }

    public IEnumerable<Aiursoft.AiurDrive.Entities.Site> Sites { get; set; } = new List<Aiursoft.AiurDrive.Entities.Site>();
}
