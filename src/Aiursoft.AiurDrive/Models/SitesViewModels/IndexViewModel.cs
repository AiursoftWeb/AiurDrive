using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.SitesViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Sites Management";
    }

    public List<SiteStorageViewModel> Sites { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}
