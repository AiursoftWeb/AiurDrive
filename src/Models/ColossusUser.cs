using Aiursoft.Gateway.SDK.Models;

namespace AiurDrive.Models
{
    public enum SiteType
    {
        WebSite,
        FileBrowser
    }

    public class ColossusUser : AiurUserBase
    {
        public string SiteName { get; set; }
        public SiteType SiteType { get; set; }
    }
}
