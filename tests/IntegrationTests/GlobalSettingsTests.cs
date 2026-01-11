using System.Net;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.AiurDrive.Configuration;
using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Services;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class GlobalSettingsTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public GlobalSettingsTests()
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = false
        };
        _port = Network.GetAvailablePort();
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://localhost:{_port}")
        };
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = await AppAsync<Startup>([], port: _port);
        await _server.UpdateDbAsync<TemplateDbContext>();
        await _server.SeedAsync();
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server == null) return;
        await _server.StopAsync();
        _server.Dispose();
    }

    private async Task LoginAsAdmin()
    {
        var response = await _http.GetAsync("/Account/Login");
        var html = await response.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex.Match(html,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />").Groups[1].Value;

        var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "EmailOrUserName", "admin" },
            { "Password", "admin123" },
            { "__RequestVerificationToken", token }
        });
        var loginResponse = await _http.PostAsync("/Account/Login", loginContent);
        Assert.AreEqual(HttpStatusCode.Found, loginResponse.StatusCode);
    }

    [TestMethod]
    public async Task TestAllowUserAdjustNicknameSetting()
    {
        // 1. Login as admin
        await LoginAsAdmin();

        // 2. Disable Allow_User_Adjust_Nickname
        using (var scope = _server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            await settingsService.UpdateSettingAsync(SettingsMap.AllowUserAdjustNickname, "False");
        }

        // 3. Verify that the "Change your profile" link is NOT visible on Manage/Index
        var manageIndexResponse = await _http.GetAsync("/Manage/Index");
        var manageIndexHtml = await manageIndexResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Change your profile", manageIndexHtml);

        // 4. Verify that accessing /Manage/ChangeProfile directly returns BadRequest
        var changeProfileResponse = await _http.GetAsync("/Manage/ChangeProfile");
        Assert.AreEqual(HttpStatusCode.BadRequest, changeProfileResponse.StatusCode);

        // 5. Enable Allow_User_Adjust_Nickname
        using (var scope = _server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            await settingsService.UpdateSettingAsync(SettingsMap.AllowUserAdjustNickname, "True");
        }

        // 6. Verify that the "Change your profile" link IS visible on Manage/Index
        manageIndexResponse = await _http.GetAsync("/Manage/Index");
        manageIndexHtml = await manageIndexResponse.Content.ReadAsStringAsync();
        Assert.Contains("Change your profile", manageIndexHtml);

        // 7. Verify that accessing /Manage/ChangeProfile directly returns OK
        changeProfileResponse = await _http.GetAsync("/Manage/ChangeProfile");
        Assert.AreEqual(HttpStatusCode.OK, changeProfileResponse.StatusCode);
    }

    [TestMethod]
    public async Task TestAdminManageSettings()
    {
        // 1. Login as admin
        await LoginAsAdmin();

        // 2. Access Global Settings Index
        var settingsResponse = await _http.GetAsync("/GlobalSettings/Index");
        settingsResponse.EnsureSuccessStatusCode();
        var settingsHtml = await settingsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Global Settings", settingsHtml);
        Assert.Contains(SettingsMap.AllowUserAdjustNickname, settingsHtml);

        // 3. Change setting via UI
        var token = System.Text.RegularExpressions.Regex.Match(settingsHtml,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />").Groups[1].Value;
        
        var editContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Key", SettingsMap.AllowUserAdjustNickname },
            { "Value", "False" },
            { "__RequestVerificationToken", token }
        });
        var editResponse = await _http.PostAsync("/GlobalSettings/Edit", editContent);
        Assert.AreEqual(HttpStatusCode.Found, editResponse.StatusCode);

        // 4. Verify setting changed in DB
        using (var scope = _server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            var value = await settingsService.GetBoolSettingAsync(SettingsMap.AllowUserAdjustNickname);
            Assert.IsFalse(value);
        }
    }
}
