using Aiursoft.AiurDrive.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class FilesPermissionTests : TestBase
{
    [TestMethod]
    public async Task TestReadOnlyShareHidesEditButtons()
    {
        // 1. Create two users
        await RegisterAndLoginAsync(); // We are now logged in as userA
        
        // Logout user A (by creating a new client/cookie container or just overwriting cookies? 
        // TestBase uses a single HttpClient with a CookieContainer.
        // To switch users, we might need to clear cookies or just register/login again which usually overrides the session cookie.
        
        // Let's create User B first, store creds, then login as User A.
        var emailB = $"userB-{Guid.NewGuid()}@aiursoft.com";
        var passwordB = "password";
        await PostForm("/Account/Register", new Dictionary<string, string>
        {
            { "Email", emailB },
            { "Password", passwordB },
            { "ConfirmPassword", passwordB }
        });

        // Now login as User A
        await RegisterAndLoginAsync();

        // 2. User A creates a site
        var siteName = "shared-repo-" + Guid.NewGuid().ToString().Substring(0, 8);
        await PostForm("/Dashboard/CreateSite", new Dictionary<string, string>
        {
            { "SiteName", siteName },
            { "OpenToUpload", "false" }, 
            { "Description", "Test Description" }
        });

        // 3. Share site with User B (ReadOnly)
        // We'll do this via DB context to avoid UI complexity of finding user ID etc.
        var db = GetService<AiurDriveDbContext>();
        var userBEntity = await db.Users.FirstAsync(u => u.Email == emailB);
        var siteEntity = await db.Sites.FirstAsync(s => s.SiteName == siteName);

        db.SiteShares.Add(new SiteShare
        {
            SiteId = siteEntity.Id,
            SharedWithUserId = userBEntity.Id,
            Permission = SharePermission.ReadOnly
        });
        await db.SaveChangesAsync();

        // 4. Login as User B
        await PostForm("/Account/Login", new Dictionary<string, string>
        {
            { "EmailOrUserName", emailB },
            { "Password", passwordB }
        });

        // 5. Access the files page
        var response = await Http.GetAsync($"/Dashboard/Files/{siteName}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // 6. Verify assertions
        StringAssert.Contains(content, siteName, "Should see site name");
        
        // Should NOT see "Upload File"
        if (content.Contains("Upload File")) Assert.Fail("Should not see 'Upload File' button");
        
        // Should NOT see "New Folder"
        if (content.Contains("New Folder")) Assert.Fail("Should not see 'New Folder' button");
        
        // Should NOT see Delete forms
        if (content.Contains($"action=\"/Dashboard/Delete/{siteName}")) Assert.Fail("Should not see delete forms");
        
        // Should NOT see Rename button usage
        if (content.Contains("onclick=\"openRenameModal")) Assert.Fail("Should not see rename buttons");

        // Should NOT see Cut button usage
        if (content.Contains("onclick=\"cutItem")) Assert.Fail("Should not see cut buttons");
    }

    [TestMethod]
    public async Task TestOwnerHasEditButtons()
    {
        // 1. Login as User A
        await RegisterAndLoginAsync();

        // 2. User A creates a site
        var siteName = "owner-repo-" + Guid.NewGuid().ToString().Substring(0, 8);
        await PostForm("/Dashboard/CreateSite", new Dictionary<string, string>
        {
            { "SiteName", siteName },
            { "OpenToUpload", "false" },
            { "Description", "Test Description" }
        });

        // 3. Access the files page
        var response = await Http.GetAsync($"/Dashboard/Files/{siteName}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // 4. Verify assertions
        StringAssert.Contains(content, siteName, "Should see site name");
        
        // Should see "Upload File" (if not full, but new site is empty)
        StringAssert.Contains(content, "Upload File", "Should see 'Upload File' button");
        
        // Should see "New Folder"
        StringAssert.Contains(content, "New Folder", "Should see 'New Folder' button");
    }
}
