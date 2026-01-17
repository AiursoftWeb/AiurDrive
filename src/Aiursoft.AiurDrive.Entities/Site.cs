using System.ComponentModel.DataAnnotations;
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
}
