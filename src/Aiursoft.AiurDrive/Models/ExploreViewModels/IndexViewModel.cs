using Aiursoft.AiurDrive.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.ExploreViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public required List<Site> PublicSites { get; set; }
}
