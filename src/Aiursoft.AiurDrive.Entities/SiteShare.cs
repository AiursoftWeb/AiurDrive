using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Aiursoft.AiurDrive.Entities;

public class SiteShare
{
    public int Id { get; set; }

    public int SiteId { get; set; }
    [ForeignKey(nameof(SiteId))]
    [JsonIgnore]
    public Site? Site { get; set; }

    public string? SharedWithUserId { get; set; }
    [ForeignKey(nameof(SharedWithUserId))]
    [JsonIgnore]
    public User? SharedWithUser { get; set; }

    // Role ID is a string in Identity
    public string? SharedWithRoleId { get; set; }
    
    public SharePermission Permission { get; set; } = SharePermission.ReadOnly;

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
}
