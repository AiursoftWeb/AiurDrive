using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Aiursoft.AiurDrive.Authorization;
using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Services.FileStorage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class SiteAdministrationTests : TestBase
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GlobalManagerCanManageAnotherUsersSiteWithoutChangingItsOwner(bool isPublic)
    {
        var site = await CreateOwnedSite(isPublic);
        var context = GetService<AiurDriveDbContext>();
        var storage = GetService<StorageService>();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("private document"));
        await storage.SaveFromStream(site.SiteName + "/secret.txt", content, isVault: true);
        await LoginWithPermission(AppPermissionNames.CanManageAnySite);

        var listing = await Http.GetAsync("/Sites");
        listing.EnsureSuccessStatusCode();
        Assert.Contains(site.SiteName, await listing.Content.ReadAsStringAsync());
        var files = await Http.GetAsync($"/Dashboard/Files/{site.SiteName}");
        files.EnsureSuccessStatusCode();
        var html = await files.Content.ReadAsStringAsync();
        Assert.Contains("secret.txt", html);
        Assert.Contains($"/Dashboard/ManageShares/{site.SiteName}", html);
        Assert.Contains($"/Dashboard/DeleteSite/{site.SiteName}", html);
        var link = Regex.Match(html, "href=\"([^\"]*download-private/[^\"]*secret\\.txt[^\"]*)\"");
        Assert.IsTrue(link.Success);
        var download = await Http.GetAsync(WebUtility.HtmlDecode(link.Groups[1].Value));
        download.EnsureSuccessStatusCode();
        Assert.AreEqual("private document", await download.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.Unauthorized,
            (await Http.GetAsync($"/download-private/{site.SiteName}/secret.txt")).StatusCode);
        foreach (var action in new[] { "EditSite", "ManageShares", "DeleteSite" })
            (await Http.GetAsync($"/Dashboard/{action}/{site.SiteName}")).EnsureSuccessStatusCode();
        AssertDenied(await Http.GetAsync($"/Sites/Edit/{site.Id}"));

        AssertRedirect(await PostForm($"/Dashboard/CreateFolder/{site.SiteName}",
            new() { ["newFolderName"] = "first" }, tokenUrl: $"/Dashboard/Files/{site.SiteName}"),
            $"/Dashboard/Files/{site.SiteName}");
        AssertRedirect(await PostForm($"/Dashboard/Rename/{site.SiteName}/first",
            new() { ["newName"] = "second" }, tokenUrl: $"/Dashboard/Files/{site.SiteName}"),
            $"/Dashboard/Files/{site.SiteName}");
        AssertRedirect(await PostForm($"/Dashboard/Move/{site.SiteName}",
            new() { ["sourcePath"] = "secret.txt", ["targetPath"] = "second" },
            tokenUrl: $"/Dashboard/Files/{site.SiteName}"), $"/Dashboard/Files/{site.SiteName}/second");
        AssertRedirect(await PostForm($"/Dashboard/Delete/{site.SiteName}/second/secret.txt",
            new(), tokenUrl: $"/Dashboard/Files/{site.SiteName}"), $"/Dashboard/Files/{site.SiteName}/second");

        AssertRedirect(await PostForm($"/Dashboard/AddShare/{site.SiteName}",
            new() { ["TargetUserId"] = site.AppUserId, ["Permission"] = "ReadOnly" },
            tokenUrl: $"/Dashboard/ManageShares/{site.SiteName}"), $"/Dashboard/ManageShares/{site.SiteName}");
        var share = await context.SiteShares.SingleAsync(s => s.SiteId == site.Id);
        AssertRedirect(await PostForm("/Dashboard/RemoveShare", new() { ["id"] = share.Id.ToString() },
            tokenUrl: $"/Dashboard/ManageShares/{site.SiteName}"), $"/Dashboard/ManageShares/{site.SiteName}");
        AssertRedirect(await PostForm($"/Dashboard/UpdateVisibility/{site.SiteName}",
            new() { ["publicAccess"] = "false" }, tokenUrl: $"/Dashboard/ManageShares/{site.SiteName}"),
            $"/Dashboard/ManageShares/{site.SiteName}");

        var newName = site.SiteName + "-new";
        AssertRedirect(await PostForm($"/Dashboard/EditSite/{site.SiteName}",
            new() { ["SiteName"] = newName, ["Description"] = "Managed by administrator", ["AllowAnonymousView"] = "false" }),
            $"/Dashboard/Files/{newName}");
        var updated = await context.Sites.AsNoTracking().SingleAsync(s => s.Id == site.Id);
        Assert.AreEqual(site.AppUserId, updated.AppUserId);
        Assert.IsFalse(updated.AllowAnonymousView);
        Assert.IsTrue(Directory.Exists(storage.GetFilePhysicalPath(newName + "/second", isVault: true)));
        AssertRedirect(await PostForm($"/Dashboard/DeleteSite/{newName}", new()), "/Dashboard");
        Assert.IsFalse(await context.Sites.AnyAsync(s => s.Id == site.Id));
        Assert.IsFalse(Directory.Exists(storage.GetFilePhysicalPath(newName, isVault: true)));
    }

    [TestMethod]
    public async Task QuotaPermissionDoesNotGrantPrivateFileOrShareAccess()
    {
        var site = await CreateOwnedSite(false);
        await LoginWithPermission(AppPermissionNames.CanOverrideSiteStorage);
        (await Http.GetAsync("/Sites")).EnsureSuccessStatusCode();
        (await Http.GetAsync($"/Sites/Edit/{site.Id}")).EnsureSuccessStatusCode();
        AssertDenied(await Http.GetAsync($"/Dashboard/Files/{site.SiteName}"));
        AssertDenied(await Http.GetAsync($"/Dashboard/ManageShares/{site.SiteName}"));
        Assert.AreEqual(HttpStatusCode.NotFound,
            (await Http.GetAsync($"/Dashboard/EditSite/{site.SiteName}")).StatusCode);
    }

    [TestMethod]
    public async Task UnrelatedUserCannotManagePrivateSites()
    {
        var site = await CreateOwnedSite(false);
        await RegisterAndLoginAsync();
        AssertDenied(await Http.GetAsync("/Sites"));
        AssertDenied(await Http.GetAsync($"/Dashboard/Files/{site.SiteName}"));
        AssertDenied(await Http.GetAsync($"/Dashboard/ManageShares/{site.SiteName}"));
        Assert.AreEqual(HttpStatusCode.NotFound,
            (await Http.GetAsync($"/Dashboard/DeleteSite/{site.SiteName}")).StatusCode);
        AssertDenied(await PostForm($"/Dashboard/UpdateVisibility/{site.SiteName}",
            new() { ["publicAccess"] = "true" }, tokenUrl: "/Dashboard/CreateSite"));
        Assert.AreEqual(HttpStatusCode.NotFound,
            (await PostForm($"/Dashboard/DeleteSite/{site.SiteName}", new(), tokenUrl: "/Dashboard/CreateSite")).StatusCode);
        using var anonymous = GetService<IHttpClientFactory>().CreateClient();
        Assert.AreEqual(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync(Http.BaseAddress + $"SharedView/Index/{site.SiteName}")).StatusCode);
    }

    [TestMethod]
    [DataRow(SharePermission.ReadOnly)]
    [DataRow(SharePermission.Editable)]
    public async Task SharingDoesNotGrantSiteAdministration(SharePermission permission)
    {
        var site = await CreateOwnedSite(false);
        var account = await RegisterAndLoginAsync();
        var context = GetService<AiurDriveDbContext>();
        var user = await context.Users.SingleAsync(u => u.Email == account.email);
        context.SiteShares.Add(new SiteShare { SiteId = site.Id, SharedWithUserId = user.Id, Permission = permission });
        await context.SaveChangesAsync();
        var response = await Http.GetAsync($"/Dashboard/Files/{site.SiteName}");
        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain($"/Dashboard/ManageShares/{site.SiteName}", await response.Content.ReadAsStringAsync());
        AssertDenied(await Http.GetAsync($"/Dashboard/ManageShares/{site.SiteName}"));
        var folder = await PostForm($"/Dashboard/CreateFolder/{site.SiteName}",
            new() { ["newFolderName"] = "shared-folder" }, tokenUrl: "/Dashboard/CreateSite");
        if (permission == SharePermission.Editable) AssertRedirect(folder, $"/Dashboard/Files/{site.SiteName}");
        else AssertDenied(folder);
    }

    [TestMethod]
    public async Task SeededAdministratorReceivesGlobalSitePermission()
    {
        var site = await CreateOwnedSite(false);
        await LoginAsAdmin();
        (await Http.GetAsync($"/Dashboard/Files/{site.SiteName}")).EnsureSuccessStatusCode();
        (await Http.GetAsync($"/Dashboard/ManageShares/{site.SiteName}")).EnsureSuccessStatusCode();
    }

    private async Task<Site> CreateOwnedSite(bool isPublic)
    {
        await RegisterAndLoginAsync();
        var name = "site-admin-" + Guid.NewGuid().ToString("N")[..12];
        AssertRedirect(await PostForm("/Dashboard/CreateSite", new()
            { ["SiteName"] = name, ["AllowAnonymousView"] = isPublic.ToString() }), $"/Dashboard/Files/{name}");
        return await GetService<AiurDriveDbContext>().Sites.AsNoTracking().SingleAsync(s => s.SiteName == name);
    }

    private async Task LoginWithPermission(string permission)
    {
        var account = await RegisterAndLoginAsync();
        var users = GetService<UserManager<User>>();
        var user = await users.FindByEmailAsync(account.email);
        Assert.IsTrue((await users.AddClaimAsync(user!, new Claim(AppPermissions.Type, permission))).Succeeded);
        AssertRedirect(await PostForm("/Account/Login", new()
            { ["EmailOrUserName"] = account.email, ["Password"] = account.password }), "/Dashboard/Index");
    }

    private static void AssertDenied(HttpResponseMessage response) => Assert.IsTrue(
        response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Found &&
        response.Headers.Location!.AbsolutePath == "/Error/Code403",
        $"Expected access denial, got {response.StatusCode}: {response.Headers.Location}");
}
