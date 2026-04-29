using Aiursoft.AiurDrive.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.ExploreViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Explore Public Sites";
    }

    public required List<Site> PublicSites { get; set; }
}
