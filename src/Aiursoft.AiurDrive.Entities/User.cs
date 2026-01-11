using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.AiurDrive.Entities;

public class User : IdentityUser
{
    public const string DefaultAvatarPath = "Workspace/avatar/default-avatar.jpg";

    [MaxLength(30)]
    [MinLength(2)]
    public required string DisplayName { get; set; }

    [MaxLength(150)] [MinLength(2)] public string AvatarRelativePath { get; set; } = DefaultAvatarPath;

    /// <summary>
    /// Total storage quota in bytes.
    /// Default is 100GB = 107374182400 bytes.
    /// </summary>
    public long TotalStorageBytes { get; set; } = 107374182400;

    public DateTime CreationTime { get; init; } = DateTime.UtcNow;
}
