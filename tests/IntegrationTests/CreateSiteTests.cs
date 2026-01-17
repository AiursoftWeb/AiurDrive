namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class CreateSiteTests : TestBase
{
    [TestMethod]
    public async Task CreateSiteAndVerifyRedirect()
    {
        // Step 1: Register and login
        await RegisterAndLoginAsync();

        // Step 2: Create a site
        var siteName = $"test-site-{Guid.NewGuid().ToString().Substring(0, 8)}";
        var createSiteResponse = await PostForm("/Dashboard/CreateSite", new Dictionary<string, string>
        {
            { "SiteName", siteName },
            { "OpenToUpload", "true" }
        });

        // Step 3: Assert redirect to Files manager
        AssertRedirect(createSiteResponse, $"/Dashboard/Files/{siteName}");

        // Step 4: Verify the Files page is accessible
        var filesPageResponse = await Http.GetAsync($"/Dashboard/Files/{siteName}");
        filesPageResponse.EnsureSuccessStatusCode();
        var html = await filesPageResponse.Content.ReadAsStringAsync();
        Assert.Contains(siteName, html);
    }
}
