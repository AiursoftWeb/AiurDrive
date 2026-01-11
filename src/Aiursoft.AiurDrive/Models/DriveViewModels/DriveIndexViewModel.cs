using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.DriveViewModels;

public class DriveIndexViewModel : UiStackLayoutViewModel
{
    public DriveIndexViewModel()
    {
        PageTitle = "File Manager";
    }

    public string? CurrentPath { get; init; }
    public DirectoryInfo[] Folders { get; init; } = Array.Empty<DirectoryInfo>();
    public FileInfo[] Files { get; init; } = Array.Empty<FileInfo>();
    public long UsedStorageBytes { get; init; }
    public long TotalStorageBytes { get; init; } = 107374182400; // 100GB default

    public double UsedStoragePercentage => TotalStorageBytes > 0 
        ? Math.Round((double)UsedStorageBytes / TotalStorageBytes * 100, 1) 
        : 0;

    public string FormattedUsedStorage => FormatBytes(UsedStorageBytes);
    public string FormattedTotalStorage => FormatBytes(TotalStorageBytes);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }
}
