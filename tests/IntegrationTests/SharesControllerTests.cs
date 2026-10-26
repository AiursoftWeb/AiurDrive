using Aiursoft.AiurDrive.Entities;
using Microsoft.EntityFrameworkCore;
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

    [TestMethod]
    public async Task PublicViewingKeepsExplicitSharesVisibleAndPreservesTheirPermissions()
    {
        await RegisterAndLoginAsync();
        var siteName = $"share-{Guid.NewGuid():N}"[..28];
        AssertRedirect(await PostForm("/Dashboard/CreateSite", new()
        {
            ["SiteName"] = siteName,
            ["AllowAnonymousView"] = "false"
        }), $"/Dashboard/Files/{siteName}");
        var db = GetService<AiurDriveDbContext>();
        var site = await db.Sites.SingleAsync(s => s.SiteName == siteName);
        var recipient = new User { UserName = "sharing-recipient", DisplayName = "Sharing recipient" };
        db.Users.Add(recipient);
        var share = new SiteShare { SiteId = site.Id, SharedWithUserId = recipient.Id, Permission = SharePermission.Editable };
        db.SiteShares.Add(share);
        await db.SaveChangesAsync();

        AssertRedirect(await PostForm($"/Dashboard/UpdateVisibility/{siteName}",
            new() { ["publicAccess"] = "true" }, tokenUrl: $"/Dashboard/ManageShares/{siteName}"),
            $"/Dashboard/ManageShares/{siteName}");
        var response = await Http.GetAsync($"/Dashboard/ManageShares/{siteName}");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(html, recipient.DisplayName);
        StringAssert.Contains(html, $"name=\"id\" value=\"{share.Id}\"");
        db.ChangeTracker.Clear();
        Assert.IsTrue((await db.Sites.SingleAsync(s => s.Id == site.Id)).AllowAnonymousView);
        Assert.AreEqual(SharePermission.Editable, (await db.SiteShares.SingleAsync(s => s.Id == share.Id)).Permission);
    }

}
