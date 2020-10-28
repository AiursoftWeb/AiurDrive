using Aiursoft.Gateway.SDK.Models;

namespace AiurDrive.Models
{
    public enum SiteType
    {
        WebSite,
        FileBrowser
    }

    public class AiurDriveUser : AiurUserBase
    {
        public string SiteName { get; set; }
        public SiteType SiteType { get; set; }
    }
}
