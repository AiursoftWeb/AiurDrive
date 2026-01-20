using Aiursoft.AiurDrive.Entities;
using Aiursoft.UiStack.Layout;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.AiurDrive.Models.SharesViewModels;

public class ManageSharesViewModel : UiStackLayoutViewModel
{
    public ManageSharesViewModel()
    {
        PageTitle = "Manage Shares";
    }
    public string SiteName { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public string? PublicLink { get; set; }
    public List<SiteShare> ExistingShares { get; set; } = new();
    public List<IdentityRole> AvailableRoles { get; set; } = new();
}