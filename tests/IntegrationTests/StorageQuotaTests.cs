using System.Net;
using System.Security.Claims;
using Aiursoft.AiurDrive.Controllers;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.AiurDrive.Configuration;
using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Services.FileStorage;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class StorageQuotaTests : TestBase
{
    private string? previousVaultQuota;

    [TestCleanup]
    public override async Task CleanServer()
    {
        if (Server != null && previousVaultQuota != null)
        {
            using var scope = Server.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<GlobalSettingsService>()
                .UpdateSettingAsync(SettingsMap.MaxVaultStorageInGB, previousVaultQuota);
        }
        await base.CleanServer();
    }

    [TestMethod]
    public async Task ConcurrentSiteUploadsCannotOverrunQuotaAndStaleGrantCannotRecreateSite()
    {
        var (email, _) = await RegisterAndLoginAsync();
        using var scope = Server!.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AiurDriveDbContext>();
        var user = await database.Users.SingleAsync(u => u.Email == email);
        var name = "quota-" + Guid.NewGuid().ToString("N");
        var site = new Site { SiteName = name, AppUserId = user.Id, StorageSizeLimit = 1 };
        database.Sites.Add(site);
        await database.SaveChangesAsync();
        var storage = GetService<StorageService>();
        var folder = storage.GetFilePhysicalPath(name, isVault: true);
        Directory.CreateDirectory(folder);
        using (var filler = File.Create(Path.Combine(folder, "existing.bin")))
            filler.SetLength(DriveStorageQuota.GiB - 6);
        var url = storage.GetUploadUrl(name, isVault: true);
        var results = await Task.WhenAll(UploadSite(url, "first.bin"), UploadSite(url, "second.bin"));
        Assert.AreEqual(1, results.Count(code => code == HttpStatusCode.OK));
        Assert.AreEqual(1, results.Count(code => code == HttpStatusCode.RequestEntityTooLarge));
        Assert.AreEqual(DriveStorageQuota.GiB - 1,
            Directory.EnumerateFiles(folder).Sum(file => new FileInfo(file).Length));
        database.Sites.Remove(site);
        await database.SaveChangesAsync();
        Directory.Delete(folder, recursive: true);
        Assert.AreEqual(HttpStatusCode.NotFound, await UploadSite(url, "orphan.bin"));
        Assert.IsFalse(Directory.Exists(folder));
    }

    [TestMethod]
    public async Task VaultOverwriteChargesOnlyGrowthAndRejectsOverQuotaWithoutDamagingObject()
    {
        var folder = await PrepareVault();
        using (var filler = File.Create(Path.Combine(folder, "existing.enc")))
            filler.SetLength(DriveStorageQuota.GiB - 6);
        Assert.AreEqual(HttpStatusCode.OK, await UploadVault("object", "1234", "m"));
        Assert.AreEqual(HttpStatusCode.OK, await UploadVault("object", "12345", "n"));
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, await UploadVault("object", "123456", "x"));
        Assert.AreEqual("12345", await File.ReadAllTextAsync(Path.Combine(folder, "object.enc")));
        Assert.AreEqual("n", await File.ReadAllTextAsync(Path.Combine(folder, "object.meta")));
        using (var metadataOnly = new MultipartFormDataContent())
        {
            metadataOnly.Add(new StringContent("too large"), "meta", "object.meta");
            Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge,
                (await Http.PostAsync("/VaultApi/Upload", metadataOnly)).StatusCode);
        }
        Assert.AreEqual("n", await File.ReadAllTextAsync(Path.Combine(folder, "object.meta")));
        Assert.AreEqual(HttpStatusCode.OK, await UploadVault("object", "1", "s"));
        Assert.AreEqual("1", await File.ReadAllTextAsync(Path.Combine(folder, "object.enc")));
        Assert.AreEqual(0, Directory.EnumerateFiles(Path.Combine(folder, ".uploads"), "*",
            SearchOption.AllDirectories).Count());
    }

    [TestMethod]
    public async Task ConcurrentVaultUploadsIncludeMetadataAndCannotOverrunQuota()
    {
        var folder = await PrepareVault();
        using (var filler = File.Create(Path.Combine(folder, "existing.enc")))
            filler.SetLength(DriveStorageQuota.GiB - 6);
        var results = await Task.WhenAll(UploadVault("one", "1234", "m"), UploadVault("two", "1234", "m"));
        Assert.AreEqual(1, results.Count(code => code == HttpStatusCode.OK));
        Assert.AreEqual(1, results.Count(code => code == HttpStatusCode.RequestEntityTooLarge));
        Assert.AreEqual(DriveStorageQuota.GiB - 1,
            Directory.EnumerateFiles(folder).Sum(file => new FileInfo(file).Length));
    }

    [TestMethod]
    public async Task ExistingVaultAboveNewQuotaCanShrinkAndDelete()
    {
        var folder = await PrepareVault();
        Assert.AreEqual(HttpStatusCode.OK, await UploadVault("object", "12345", "m"));
        using var scope = Server!.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<GlobalSettingsService>()
            .UpdateSettingAsync(SettingsMap.MaxVaultStorageInGB, "0");
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, await UploadVault("other", "1", "m"));
        Assert.AreEqual(HttpStatusCode.OK, await UploadVault("object", "1", "m"));
        Assert.AreEqual(HttpStatusCode.OK, (await Http.DeleteAsync("/VaultApi/Delete/object")).StatusCode);
        Assert.IsFalse(File.Exists(Path.Combine(folder, "object.enc")));
        Assert.AreEqual(HttpStatusCode.OK, (await Http.PostAsync("/VaultApi/Reset", null)).StatusCode);
    }

    [TestMethod]
    public async Task FailedVaultUploadPreservesBothOriginalParts()
    {
        var folder = await PrepareVault();
        Assert.AreEqual(HttpStatusCode.OK, await UploadVault("object", "original", "metadata"));
        using var scope = Server!.Services.CreateScope();
        var controller = ActivatorUtilities.CreateInstance<VaultApiController>(scope.ServiceProvider);
        var id = new DirectoryInfo(folder).Parent!.Name;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, id)], "Test"))
            }
        };
        await using var metadata = new MemoryStream("new"u8.ToArray());
        var form = new FormFile(metadata, 0, metadata.Length, "meta", "object.meta");
        await Assert.ThrowsAsync<IOException>(() => controller.Upload(new BrokenUpload(), form));
        Assert.AreEqual("original", await File.ReadAllTextAsync(Path.Combine(folder, "object.enc")));
        Assert.AreEqual("metadata", await File.ReadAllTextAsync(Path.Combine(folder, "object.meta")));
        Assert.AreEqual(0, Directory.EnumerateFiles(Path.Combine(folder, ".uploads"), "*",
            SearchOption.AllDirectories).Count());
    }

    private sealed class BrokenUpload : IFormFile
    {
        public string ContentType => "application/octet-stream";
        public string ContentDisposition => "form-data";
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length => 4;
        public string Name => "file";
        public string FileName => "object.enc";
        public Stream OpenReadStream() => throw new IOException("Interrupted upload.");
        public void CopyTo(Stream target) => throw new IOException("Interrupted upload.");
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) =>
            Task.FromException(new IOException("Interrupted upload."));
    }

    private async Task<string> PrepareVault()
    {
        var (email, _) = await RegisterAndLoginAsync();
        using var scope = Server!.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
        previousVaultQuota = await settings.GetSettingValueAsync(SettingsMap.MaxVaultStorageInGB);
        await settings.UpdateSettingAsync(SettingsMap.MaxVaultStorageInGB, "1");
        var user = await scope.ServiceProvider.GetRequiredService<AiurDriveDbContext>().Users
            .SingleAsync(u => u.Email == email);
        var folder = Path.Combine(GetService<FeatureFoldersProvider>().GetVaultFolder(), "_ZeroTrust", user.Id, "objects");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private async Task<HttpStatusCode> UploadSite(string url, string name)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("12345"), "file", name);
        using var response = await Http.PostAsync(url, form);
        return response.StatusCode;
    }

    private async Task<HttpStatusCode> UploadVault(string id, string content, string metadata)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(content), "file", id + ".enc");
        form.Add(new StringContent(metadata), "meta", id + ".meta");
        using var response = await Http.PostAsync("/VaultApi/Upload", form);
        return response.StatusCode;
    }
}
