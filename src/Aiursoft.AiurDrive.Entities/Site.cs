using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Aiursoft.CSTools.Attributes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Aiursoft.AiurDrive.Entities;

[Index(nameof(SiteName), IsUnique = true)]
public class Site
{
    public int Id { get; set; }

    [ValidDomainName]
    public required string SiteName { get; set; }

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    public required string AppUserId { get; set; }
    
    [JsonIgnore]
    public User? AppUser { get; set; }


    /// <summary>
    /// If true, anonymous users can view (read-only) the site's files.
    /// If false, only the owner and users with explicit shares can access.
    /// All sites are stored in Vault regardless of this setting.
    /// </summary>
    public bool AllowAnonymousView { get; set; } = false;

    [MaxLength(300)]
    public string? Description { get; set; }
    
    public long? StorageSizeLimit { get; set; }

    [InverseProperty(nameof(SiteShare.Site))]
    public IEnumerable<SiteShare> SiteShares { get; set; } = new List<SiteShare>();
}
