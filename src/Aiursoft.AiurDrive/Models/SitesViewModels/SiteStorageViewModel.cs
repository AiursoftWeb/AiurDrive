using Aiursoft.AiurDrive.Entities;

namespace Aiursoft.AiurDrive.Models.SitesViewModels;

public class SiteStorageViewModel
{
    public required Site Site { get; set; }
    public long UsedSizeInBytes { get; set; }
    public long LimitInGB { get; set; }
    public bool IsOverridden { get; set; }
}
