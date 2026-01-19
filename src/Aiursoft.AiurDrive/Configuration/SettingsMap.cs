using Aiursoft.AiurDrive.Models;

namespace Aiursoft.AiurDrive.Configuration;

public static class SettingsMap
{
    public const string AllowUserAdjustNickname = "Allow_User_Adjust_Nickname";
    public const string MaxSiteStorageInGB = "Max_Site_Storage_In_GB";
    public const string MaxSitesPerPerson = "Max_Sites_Per_Person";

    /// <summary>
    /// If true, image preview will be enabled in the file manager. This is only a front-end switch. Image processing is always enabled.
    /// </summary>
    public const string AllowImagePreview = "Allow_Image_Preview";

    public class FakeLocalizer
    {
        public string this[string name] => name;
    }

    private static readonly FakeLocalizer Localizer = new();

    public static readonly List<GlobalSettingDefinition> Definitions = new()
    {
        new GlobalSettingDefinition
        {
            Key = AllowUserAdjustNickname,
            Description = Localizer["Allow users to adjust their nickname in the profile management page."],
            Type = SettingType.Bool,
            DefaultValue = "True"
        },
        new GlobalSettingDefinition
        {
            Key = MaxSiteStorageInGB,
            Description = Localizer["Max storage size for each site in GB."],
            Type = SettingType.Number,
            DefaultValue = "10"
        },
        new GlobalSettingDefinition
        {
            Key = MaxSitesPerPerson,
            Description = Localizer["Maximum number of sites each user can create."],
            Type = SettingType.Number,
            DefaultValue = "5"
        },
        new GlobalSettingDefinition
        {
            Key = AllowImagePreview,
            Description = "Allow image preview in the file manager. This is only a front-end switch. Image processing is always enabled.",
            Type = SettingType.Bool,
            DefaultValue = "True"
        }
    };
}
