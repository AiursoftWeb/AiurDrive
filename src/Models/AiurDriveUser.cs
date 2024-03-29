﻿using Aiursoft.Directory.SDK.Models;

namespace Aiursoft.AiurDrive.Models
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
