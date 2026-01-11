using Aiursoft.UiStack.Layout;

namespace Aiursoft.AiurDrive.Models.GlobalSettingsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Global Settings";
    }

    public List<SettingViewModel> Settings { get; set; } = new();
}

public class SettingViewModel
{
    public required string Key { get; set; }
    public required string Description { get; set; }
    public required SettingType Type { get; set; }
    public string? Value { get; set; }
    public required string DefaultValue { get; set; }
    public bool IsOverriddenByConfig { get; set; }
    public Dictionary<string, string>? ChoiceOptions { get; set; }
}
