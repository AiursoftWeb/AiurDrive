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

    public class FakeLocalizer
    {
        public string this[string name] => name;
    }

    private static readonly FakeLocalizer Localizer = new();

    public static readonly List<GlobalSettingDefinition> Definitions = new()
    {
        new GlobalSettingDefinition
        {
            Key = "ProjectName",
            Name = Localizer["Project Name"],
            Description = Localizer["The name of the project displayed in the frontend."],
            Type = SettingType.Text,
            DefaultValue = "Aiursoft AiurDrive"
        },
        new GlobalSettingDefinition
        {
            Key = "BrandName",
            Name = Localizer["Brand Name"],
            Description = Localizer["The brand name of the company or project. E.g. Aiursoft."],
            Type = SettingType.Text,
            DefaultValue = "Aiursoft"
        },
        new GlobalSettingDefinition
        {
            Key = "BrandHomeUrl",
            Name = Localizer["Brand Home URL"],
            Description = Localizer["The URL of the company or project. E.g. https://www.aiursoft.com"],
            Type = SettingType.Text,
            DefaultValue = "https://www.aiursoft.com"
        },
        new GlobalSettingDefinition
        {
            Key = AllowUserAdjustNickname,
            Name = Localizer["Allow User Adjust Nickname"],
            Description = Localizer["Allow users to adjust their nickname in the profile management page."],
            Type = SettingType.Bool,
            DefaultValue = "True"
        },
        new GlobalSettingDefinition
        {
            Key = MaxSiteStorageInGB,
            Name = Localizer["Max Site Storage In GB"],
            Description = Localizer["Max storage size for each site in GB."],
            Type = SettingType.Number,
            DefaultValue = "10"
        },
        new GlobalSettingDefinition
        {
            Key = MaxSitesPerPerson,
            Name = Localizer["Max Sites Per Person"],
            Description = Localizer["Maximum number of sites each user can create."],
            Type = SettingType.Number,
            DefaultValue = "5"
        },
        new GlobalSettingDefinition
        {
            Key = AllowImagePreview,
            Name = Localizer["Allow Image Preview"],
            Description = Localizer["Allow image preview in the file manager. This is only a front-end switch. Image processing is always enabled."],
            Type = SettingType.Bool,
            DefaultValue = "True"
        },
        new GlobalSettingDefinition
        {
            Key = "ProjectLogo",
            Name = Localizer["Project Logo"],
            Description = Localizer["The logo of the project displayed in the navbar and footer. Support jpg, png, svg."],
            Type = SettingType.File,
            DefaultValue = "",
            Subfolder = "project-logo",
            AllowedExtensions = "jpg png svg",
            MaxSizeInMb = 5
        }
    };
}
