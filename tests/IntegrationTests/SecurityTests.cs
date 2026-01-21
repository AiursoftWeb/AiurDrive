using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Services.FileStorage;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class SecurityTests : TestBase
{
    [TestMethod]
    public async Task TestCrossSiteMove()
    {
        // 1. Setup User A and Site A
        await LoginAsAdmin(); // Acts as User A
        var context = GetService<AiurDriveDbContext>();
        var userA = await context.Users.FirstAsync(u => u.Email == "admin@default.com");
        
        var siteA = new Site
        {
            SiteName = "site-a-" + Guid.NewGuid().ToString().Substring(0, 5).ToLower(),
            AppUserId = userA.Id
        };
        context.Sites.Add(siteA);
        await context.SaveChangesAsync();

        // 2. Setup User B and Site B
        // We'll simulate User B by creating another user and site directly in DB
        var userB = new User
        {
            UserName = "victim@aiursoft.com",
            Email = "victim@aiursoft.com",
            DisplayName = "Victim"
        };
        context.Users.Add(userB);
        await context.SaveChangesAsync(); // Get ID

        var siteB = new Site
        {
            SiteName = "site-b-" + Guid.NewGuid().ToString().Substring(0, 5).ToLower(),
            AppUserId = userB.Id
        };
        context.Sites.Add(siteB);
        await context.SaveChangesAsync();

        var storage = GetService<StorageService>();
        // Ensure Site B folder exists
        var physicalPathB_Root = storage.GetFilePhysicalPath(siteB.SiteName, isVault: true);
        if (!Directory.Exists(physicalPathB_Root)) Directory.CreateDirectory(physicalPathB_Root);

        // 3. User A creates a file in Site A
        // We can cheat and physically create the file since we are in integration tests
        
        // Ensure folder exists
        var physicalPathA = storage.GetFilePhysicalPath(siteA.SiteName, isVault: true);
        if (!Directory.Exists(physicalPathA)) Directory.CreateDirectory(physicalPathA);
        
        var fileName = "secret.txt";
        var filePathA = Path.Combine(physicalPathA, fileName);
        await File.WriteAllTextAsync(filePathA, "This is a secret file");

        // 4. User A attempts to move this file to Site B via Path Traversal
        // Site Name: siteA
        // Source: secret.txt
        // Target: ../siteB
        // Logic in Controller: Path.Combine(siteName, targetPath, fileName)
        // Resulting Logical Dest: siteA/../siteB/secret.txt -> siteB/secret.txt
        
        var targetPath = $"../{siteB.SiteName}";
        
        var formData = new Dictionary<string, string>
        {
            { "sourcePath", fileName },
            { "targetPath", targetPath }
        };

        var response = await PostForm($"/Dashboard/Move/{siteA.SiteName}", formData);
        
        // 5. Verify Result
        // If vulnerable, the file is moved to Site B
        var physicalPathB = storage.GetFilePhysicalPath(siteB.SiteName, isVault: true);
        var filePathB = Path.Combine(physicalPathB, fileName);
        
        bool fileMoved = File.Exists(filePathB);
        bool fileExistsOriginal = File.Exists(filePathA);

        if (fileMoved && !fileExistsOriginal)
        {
            Assert.Fail("VULNERABILITY CONFIRMED: File was successfully moved to another user's site!");
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
             // This is good
        }
        else 
        {
            // check if file moved regardless of status code?
            if (fileMoved) Assert.Fail("VULNERABILITY CONFIRMED: File moved.");
        }
    }
    [TestMethod]
    public async Task TestPrefixOverlapAttack()
    {
        // Scenario: Hacker owns site "aa", Victim owns site "aaa".
        // Hacker tries to use a token for "aa" to access "aaa".
        
        await LoginAsAdmin();
        var context = GetService<AiurDriveDbContext>();
        var userHacker = new User
        {
            UserName = "hacker@aiursoft.com",
            Email = "hacker@aiursoft.com",
            DisplayName = "Hacker"
        };
        context.Users.Add(userHacker);
        
        var userVictim = new User
        {
            UserName = "victim2@aiursoft.com",
            Email = "victim2@aiursoft.com",
            DisplayName = "Victim2"
        };
        context.Users.Add(userVictim);
        await context.SaveChangesAsync();
        
        var siteHacker = new Site { SiteName = "aa", AppUserId = userHacker.Id }; // Prefix
        var siteVictim = new Site { SiteName = "aaa", AppUserId = userVictim.Id }; // Victim
        context.Sites.Add(siteHacker);
        context.Sites.Add(siteVictim);
        await context.SaveChangesAsync();
        
        // Ensure folders exist
        var storage = GetService<StorageService>();
        var pathHacker = storage.GetFilePhysicalPath(siteHacker.SiteName, isVault: false);
        var pathVictim = storage.GetFilePhysicalPath(siteVictim.SiteName, isVault: false);
        Directory.CreateDirectory(pathHacker);
        Directory.CreateDirectory(pathVictim);

        // Hacker generates a valid upload token for their site "aa"
        var token = storage.GetToken(siteHacker.SiteName, FilePermission.Upload);
        
        // Hacker tries to use this token to upload to "aaa"
        // If "StartsWith" logic is flawed (e.g. "aaa".StartsWith("aa")), this might work.
        // It SHOULD fail because safe implementation checks for "aa/"
        
        bool isValid = storage.ValidateToken(siteVictim.SiteName, token, FilePermission.Upload);
        Assert.IsFalse(isValid, "Token for 'aa' should not be valid for 'aaa'");
    }

    [TestMethod]
    public async Task TestCrossSiteUploadWithoutToken()
    {
        await LoginAsAdmin();
        var context = GetService<AiurDriveDbContext>();
        var user = await context.Users.FirstAsync(u => u.Email == "admin@default.com");
        var site = new Site { SiteName = "public-site-" + Guid.NewGuid().ToString().Substring(0, 5), AppUserId = user.Id };
        context.Sites.Add(site);
        await context.SaveChangesAsync();
        
        // Try to upload without token
        var dummyFile = new FormFile(new MemoryStream(new byte[10]), 0, 10, "file", "test.txt");
        var response = await Http.PostAsync($"/upload/{site.SiteName}", new MultipartFormDataContent
        {
            { new StreamContent(dummyFile.OpenReadStream()), "file", "test.txt" }
        });
        
        // Should be Unauthorized because token is missing
        Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public void TestTokenScope()
    {
        var storage = GetService<StorageService>();
        var path = "some/path";
        
        // Generate Download Token
        var downloadToken = storage.GetToken(path, FilePermission.Download);
        
        // Try to use for Upload
        var isValidForUpload = storage.ValidateToken(path, downloadToken, FilePermission.Upload);
        Assert.IsFalse(isValidForUpload, "Download token should not be valid for Upload");
        
        // Try to use for Download (Should be valid)
        var isValidForDownload = storage.ValidateToken(path, downloadToken, FilePermission.Download);
        Assert.IsTrue(isValidForDownload, "Download token should be valid for Download");
    }
}
