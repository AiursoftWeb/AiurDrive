using System.Net;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class SharesControllerTests : TestBase
{
    [TestMethod]
    public async Task PublicShareLinkUsesAnonymousSharedViewAndIsAccessibleWithoutLogin()
    {
        await RegisterAndLoginAsync();

        var siteName = $"public-share-{Guid.NewGuid():N}"[..28];
        var createSiteResponse = await PostForm("/Dashboard/CreateSite", new Dictionary<string, string>
        {
            { "SiteName", siteName },
            { "AllowAnonymousView", "true" }
        });
        AssertRedirect(createSiteResponse, $"/Dashboard/Files/{siteName}");

        var manageResponse = await Http.GetAsync($"/Dashboard/ManageShares/{siteName}");
        manageResponse.EnsureSuccessStatusCode();
        var managePage = await manageResponse.Content.ReadAsStringAsync();
        var expectedPublicLink = $"http://localhost:{Port}/SharedView/Index/{siteName}";

        StringAssert.Contains(managePage, $"value=\"{expectedPublicLink}\"");
        Assert.IsFalse(
            managePage.Contains($"value=\"http://localhost:{Port}/Dashboard/Files/{siteName}\"", StringComparison.Ordinal),
            "The public link must not point to the authenticated Dashboard route.");

        await PostForm("/Account/LogOff", new Dictionary<string, string>(), includeToken: false);

        var sharedViewResponse = await Http.GetAsync(expectedPublicLink);

        Assert.AreEqual(HttpStatusCode.OK, sharedViewResponse.StatusCode);
        var sharedViewPage = await sharedViewResponse.Content.ReadAsStringAsync();
        StringAssert.Contains(sharedViewPage, $"Shared - {siteName}");
    }
}
