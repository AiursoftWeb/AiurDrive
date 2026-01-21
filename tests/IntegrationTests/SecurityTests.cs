using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.CSTools.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task TestTokenScope()
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
    [TestMethod]
    public async Task TestCrossSiteDelete()
    {
        await LoginAsAdmin();
        var context = GetService<AiurDriveDbContext>();
        var userA = await context.Users.FirstAsync(u => u.Email == "admin@default.com");
        
        // Ensure User B exists
        var userB = await context.Users.FirstOrDefaultAsync(u => u.UserName == "victim-del@aiursoft.com");
        if (userB == null)
        {
            userB = new User { UserName = "victim-del@aiursoft.com", Email = "victim-del@aiursoft.com", DisplayName = "VictimDel" };
            context.Users.Add(userB);
            await context.SaveChangesAsync();
        }

        // Site A (Attacker)
        var siteA = new Site { SiteName = "site-a-del-" + Guid.NewGuid().ToString().Substring(0, 5), AppUserId = userA.Id };
        // Site B (Victim)
        var siteB = new Site { SiteName = "site-b-del-" + Guid.NewGuid().ToString().Substring(0, 5), AppUserId = userB.Id };
        
        context.Sites.Add(siteA);
        context.Sites.Add(siteB);
        await context.SaveChangesAsync();
        
        var storage = GetService<StorageService>();
        var pathB = storage.GetFilePhysicalPath(siteB.SiteName, isVault: true);
        Directory.CreateDirectory(pathB);
        var fileInB = Path.Combine(pathB, "victim.txt");
        await File.WriteAllTextAsync(fileInB, "data");
        
        // Attacker attempts path traversal delete via Site A
        // Use encoded dot-dot to ensure it reaches the controller as strict string if possible, 
        // OR rely on the fact that if it normalizes, it accesses SiteB which is now forbidden.
        // We want to test the '..' check specifically, so let's try to bypass normalization by using a folder.
        // But HttpClient normalizes paths.
        // Let's use %2E%2E to ensure it passes as parameter.
        
        var traversalPath = $"%2E%2E/{siteB.SiteName}/victim.txt";
        
        var response = await PostForm($"/Dashboard/Delete/{siteA.SiteName}/{traversalPath}", new Dictionary<string, string>());
        
        // Check if file is deleted (Ultimate Truth)
        if (!File.Exists(fileInB))
        {
             Assert.Fail($"VULNERABILITY CONFIRMED: File in {siteB.SiteName} was deleted via {siteA.SiteName}");
        }
        
        // Also check status code isn't success-like, to ensure we aren't silently failing in a weird way
        // But primarily rely on file existence. 
        // We expect 400 (if blocked) or 403 (if auth) or 404 (if not found).
        // 302 Found usually means success redirect.
        if (response.StatusCode == System.Net.HttpStatusCode.Found)
        {
             // If redirected, check where. If back to files, it implies success flow?
             // But if file exists, it means operation failed essentially.
        }
    }

    [TestMethod]
    public async Task TestCrossSiteCreateFolder()
    {
        await LoginAsAdmin();
        var context = GetService<AiurDriveDbContext>();
        var userA = await context.Users.FirstAsync(u => u.Email == "admin@default.com");
        
        var userB = await context.Users.FirstOrDefaultAsync(u => u.UserName == "victim-cr@aiursoft.com");
        if (userB == null)
        {
             userB = new User { UserName = "victim-cr@aiursoft.com", Email = "victim-cr@aiursoft.com", DisplayName = "VictimCr" };
             context.Users.Add(userB);
             await context.SaveChangesAsync();
        }

        var siteA = new Site { SiteName = "site-a-create-" + Guid.NewGuid().ToString().Substring(0, 5), AppUserId = userA.Id };
        var siteB = new Site { SiteName = "site-b-create-" + Guid.NewGuid().ToString().Substring(0, 5), AppUserId = userB.Id };
        context.Sites.Add(siteA);
        context.Sites.Add(siteB);
        await context.SaveChangesAsync();
        
        var storage = GetService<StorageService>();
        var pathB = storage.GetFilePhysicalPath(siteB.SiteName, isVault: true);
        Directory.CreateDirectory(pathB);
        
        var traversalPath = $"%2E%2E/{siteB.SiteName}";
        var newFolder = "hacked_folder";
        
        var response = await PostForm($"/Dashboard/CreateFolder/{siteA.SiteName}/{traversalPath}", new Dictionary<string, string>
        {
            { "newFolderName", newFolder }
        });
        
        // Check if folder is created (Ultimate Truth)
        var hackedFolderPath = Path.Combine(pathB, newFolder);
        if (Directory.Exists(hackedFolderPath))
        {
             Assert.Fail($"VULNERABILITY CONFIRMED: Folder created in {siteB.SiteName} via {siteA.SiteName}");
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.Found)
        {
             // If redirected, could be success flow, but if directory not created, we are safe.
        }
    }
}
