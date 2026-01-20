using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.DashboardViewModels;

public class FileManagerViewModel : UiStackLayoutViewModel
{
    public FileManagerViewModel()
    {
        PageTitle = "File Manager";
        Files = new List<FileInfo>();
        Folders = new List<string>();
    }

    public required string SiteName { get; set; }
    public bool OpenToUpload { get; set; }
    public string Path { get; set; } = string.Empty;
    public List<FileInfo> Files { get; set; }
    public List<string> Folders { get; set; }
    public string? UploadedFilePath { get; set; }

    public long UsedSpaceInBytes { get; set; }
    public long TotalSpaceInGB { get; set; }
    public bool AllowImagePreview { get; set; }
    public bool IsOwner { get; set; }
}
