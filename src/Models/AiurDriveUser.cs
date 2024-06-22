using Microsoft.AspNetCore.Identity;

namespace Aiursoft.AiurDrive.Models
{
    public enum SiteType
    {
        WebSite,
        FileBrowser
    }

    public class AiurDriveUser : IdentityUser
    {
        public string SiteName { get; set; }
        public SiteType SiteType { get; set; }
    }
}
