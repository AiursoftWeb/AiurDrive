using Aiursoft.AiurDrive.Models;

namespace Aiursoft.AiurDrive.Configuration;

public class SettingsMap
{
    public const string AllowUserAdjustNickname = "Allow_User_Adjust_Nickname";
    public const string MaxSiteStorageInGB = "Max_Site_Storage_In_GB";
    public const string MaxSitesPerPerson = "Max_Sites_Per_Person";

    /// <summary>
    /// If true, image preview will be enabled in the file manager. This is only a front-end switch. Image processing is always enabled.
    /// </summary>
    public const string AllowImagePreview = "Allow_Image_Preview";



    public static readonly List<GlobalSettingDefinition> Definitions = new()
    {
        new GlobalSettingDefinition
        {
            Key = AllowUserAdjustNickname,
            Name = "Allow User Adjust Nickname",
            Description = "Allow users to adjust their nickname in the profile management page.",
            Type = SettingType.Bool,
            DefaultValue = "True"
        },
        new GlobalSettingDefinition
        {
            Key = MaxSiteStorageInGB,
            Name = "Max Site Storage In GB",
            Description = "Max storage size for each site in GB.",
            Type = SettingType.Number,
            DefaultValue = "10"
        },
        new GlobalSettingDefinition
        {
            Key = MaxSitesPerPerson,
            Name = "Max Sites Per Person",
            Description = "Maximum number of sites each user can create.",
            Type = SettingType.Number,
            DefaultValue = "5"
        },
        new GlobalSettingDefinition
        {
            Key = AllowImagePreview,
            Name = "Allow Image Preview",
            Description = "Allow image preview in the file manager. This is only a front-end switch. Image processing is always enabled.",
            Type = SettingType.Bool,
            DefaultValue = "True"
        }
    };
}
