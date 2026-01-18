using Aiursoft.AiurDrive.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class ExploreTests : TestBase
{
    [TestMethod]
    public async Task TestExplorePage()
    {
        // 1. Visit Explore Page (should be accessible by anyone)
        var response = await Http.GetAsync("/Explore");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(content, "Explore Public Sites");
    }

    [TestMethod]
    public async Task TestSharedViewAccess()
    {
        // 1. Create a public site manually in DB
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var user = await db.Users.FirstAsync();
            var publicSite = new Site
            {
                SiteName = "public-test-site",
                AppUserId = user.Id,
                OpenToUpload = true
            };
            db.Sites.Add(publicSite);
            await db.SaveChangesAsync();
        }

        // 2. Access Shared View
        var response = await Http.GetAsync("/SharedView/Index/public-test-site");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(content, "Shared - public-test-site");
        StringAssert.Contains(content, "Site Information");
    }

    [TestMethod]
    public async Task TestPrivateSiteInSharedView()
    {
        // 1. Create a private site manually in DB
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var user = await db.Users.FirstAsync();
            var privateSite = new Site
            {
                SiteName = "private-test-site",
                AppUserId = user.Id,
                OpenToUpload = false
            };
            db.Sites.Add(privateSite);
            await db.SaveChangesAsync();
        }

        // 2. Try to access Shared View (should be Unauthorized)
        var response = await Http.GetAsync("/SharedView/Index/private-test-site");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
