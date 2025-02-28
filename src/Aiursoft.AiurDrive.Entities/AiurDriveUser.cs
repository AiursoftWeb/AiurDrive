using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.AiurDrive.Entities
{
    public class AiurDriveUser : IdentityUser
    {
        // ReSharper disable once UnusedMember.Global
        [MaxLength(100)]
        public string? SiteName { get; set; }

        public SiteType SiteType { get; set; }
    }
}
