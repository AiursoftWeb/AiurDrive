using Aiursoft.AiurDrive.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.SharedViewModels;

public class SharedViewModel : UiStackLayoutViewModel
{
    public required Site Site { get; set; }
    public string Path { get; set; } = string.Empty;
    public List<FileInfo> Files { get; set; } = new();
    public List<string> Folders { get; set; } = new();
    public bool AllowImagePreview { get; set; }
}
