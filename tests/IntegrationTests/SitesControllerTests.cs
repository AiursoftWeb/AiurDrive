using System.Net;
using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Models.SitesViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class SitesControllerTests : TestBase
{
    [TestMethod]
    public async Task TestIndex()
    {
        await LoginAsAdmin();
        var response = await Http.GetAsync("/Sites/Index");
        response.EnsureSuccessStatusCode();
    }
    
    [TestMethod]
    public async Task TestEdit()
    {
        await LoginAsAdmin();
        
        // Create a test site first
        var context = GetService<AiurDriveDbContext>();
        var user = await context.Users.FirstAsync();
        var site = new Site
        {
            SiteName = "testsiteforoverride",
            AppUserId = user.Id,
            OpenToUpload = true
        };
        context.Sites.Add(site);
        await context.SaveChangesAsync();
        
        // Get Edit Page
        var response = await Http.GetAsync($"/Sites/Edit/{site.Id}");
        response.EnsureSuccessStatusCode();
        
        // Post Edit
        var formData = new Dictionary<string, string>
        {
            { "SiteId", site.Id.ToString() },
            { "SiteName", site.SiteName },
            { "StorageSizeLimit", "100" }
        };
        
        response = await PostForm($"/Sites/Edit/{site.Id}", formData);
        AssertRedirect(response, "/Sites", exact: false);
        
        // Verify changes
        var context2 = GetService<AiurDriveDbContext>();
        var updatedSite = await context2.Sites.FindAsync(site.Id);
        await context2.Entry(updatedSite!).ReloadAsync();
        Assert.IsNotNull(updatedSite);
        Assert.AreEqual(100, updatedSite.StorageSizeLimit);
    }
}
