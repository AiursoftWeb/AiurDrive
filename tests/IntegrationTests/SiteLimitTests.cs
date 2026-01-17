using System.Net;
using Aiursoft.AiurDrive.Configuration;
using Aiursoft.AiurDrive.Services;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class SiteLimitTests : TestBase
{
    [TestMethod]
    public async Task TestSiteLimitEnforcement()
    {
        // 1. Register and login
        await RegisterAndLoginAsync();

        // 2. Set limit to 2 for testing
        using (var scope = Server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            await settingsService.UpdateSettingAsync(SettingsMap.MaxSitesPerPerson, "2");
        }

        // 3. Create 2 sites
        for (int i = 1; i <= 2; i++)
        {
            var createResponse = await PostForm("/Dashboard/CreateSite", new Dictionary<string, string>
            {
                { "SiteName", $"test-site-{i}" },
                { "OpenToUpload", "true" }
            });
            AssertRedirect(createResponse, $"/Dashboard/Files/test-site-{i}");
        }

        // 4. Try to create the 3rd site (GET) - should redirect to Index
        var getCreateResponse = await Http.GetAsync("/Dashboard/CreateSite");
        AssertRedirect(getCreateResponse, "/Dashboard");

        // 5. Try to create the 3rd site (POST) - should stay on page with error or redirect
        // In my implementation, it adds a model error and returns the view if POSTed directly.
        // But if GET redirects, they shouldn't even reach the form.
        var postCreateResponse = await PostForm("/Dashboard/CreateSite", new Dictionary<string, string>
        {
            { "SiteName", "test-site-3" },
            { "OpenToUpload", "true" }
        });
        
        // It returns the view (200) because it adds a ModelState error.
        Assert.AreEqual(HttpStatusCode.OK, postCreateResponse.StatusCode);
        var content = await postCreateResponse.Content.ReadAsStringAsync();
        StringAssert.Contains(content, "reached the maximum number of sites");

        // 6. Increase limit to 3
        using (var scope = Server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            await settingsService.UpdateSettingAsync(SettingsMap.MaxSitesPerPerson, "3");
        }

        // 7. Now should be able to create the 3rd site
        var finalCreateResponse = await PostForm("/Dashboard/CreateSite", new Dictionary<string, string>
        {
            { "SiteName", "test-site-3" },
            { "OpenToUpload", "true" }
        });
        AssertRedirect(finalCreateResponse, "/Dashboard/Files/test-site-3");
    }
}
