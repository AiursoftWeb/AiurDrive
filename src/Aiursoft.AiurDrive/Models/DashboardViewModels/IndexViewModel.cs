using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Dashboard";
    }

    public IEnumerable<Entities.Site> Sites { get; set; } = new List<Entities.Site>();
    public int MaxSites { get; set; }
    public int CurrentSitesCount { get; set; }
}
