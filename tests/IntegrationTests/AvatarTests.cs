using System.Net;
using System.Text.RegularExpressions;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.AiurDrive.Entities;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

// JB scanner bug. Not a warning.
#pragma warning disable CS8602

[TestClass]
public class AvatarTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public AvatarTests()
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = false
        };
        _port = Network.GetAvailablePort();
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://localhost:{_port}")
        };
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = await AppAsync<Startup>([], port: _port);
        await _server.UpdateDbAsync<TemplateDbContext>();
        await _server.SeedAsync();
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server == null) return;
        await _server.StopAsync();
        _server.Dispose();
    }

    private async Task<string> GetAntiCsrfToken(string url)
    {
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find anti-CSRF token on page: {url}");
        }

        return match.Groups[1].Value;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private async Task<(string email, string password)> RegisterAndLoginAsync()
    {
        var email = $"test-{Guid.NewGuid()}@aiursoft.com";
        var password = "Test-Password-123";

        var registerToken = await GetAntiCsrfToken("/Account/Register");
        var registerContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", password },
            { "ConfirmPassword", password },
            { "__RequestVerificationToken", registerToken }
        });
        var registerResponse = await _http.PostAsync("/Account/Register", registerContent);
        Assert.AreEqual(HttpStatusCode.Found, registerResponse.StatusCode);

        return (email, password);
    }

    [TestMethod]
    public async Task ChangeAvatarSuccessfullyTest()
    {
        // 1. Register and Login
        await RegisterAndLoginAsync();

        // 2. Upload a file
        // We need a valid image file. We can create a small dummy image or just use a text file if the server doesn't strictly validate image content (ManageController does check IsValidImageAsync).
        // Since we don't have a real image, we might fail the IsValidImageAsync check if we upload text.
        // However, let's try to upload a small valid GIF or PNG bytes.
        // 1x1 transparent GIF
        var gifBytes = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
        var fileContent = new ByteArrayContent(gifBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif");

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(fileContent, "file", "avatar.gif");

        var uploadResponse = await _http.PostAsync("/upload/avatars", multipartContent);
        uploadResponse.EnsureSuccessStatusCode();

        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();
        Assert.IsNotNull(uploadResult);
        Assert.IsNotNull(uploadResult.Path);

        // 3. Change Avatar
        var changeAvatarToken = await GetAntiCsrfToken("/Manage/ChangeAvatar");
        var changeAvatarContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "AvatarUrl", uploadResult.Path },
            { "__RequestVerificationToken", changeAvatarToken }
        });

        var changeAvatarResponse = await _http.PostAsync("/Manage/ChangeAvatar", changeAvatarContent);

        // 4. Verify Success
        Assert.AreEqual(HttpStatusCode.Found, changeAvatarResponse.StatusCode);
        Assert.AreEqual("/Manage?Message=ChangeAvatarSuccess", changeAvatarResponse.Headers.Location?.OriginalString);
    }

    [TestMethod]
    public async Task AvatarImageProcessingTest()
    {
        // 1. Register and Login
        await RegisterAndLoginAsync();

        // 2. Upload a file
        var gifBytes = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
        var fileContent = new ByteArrayContent(gifBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif");

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(fileContent, "file", "avatar.gif");

        var uploadResponse = await _http.PostAsync("/upload/avatars", multipartContent);
        uploadResponse.EnsureSuccessStatusCode();

        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();
        Assert.IsNotNull(uploadResult);
        Assert.IsNotNull(uploadResult.InternetPath);

        // 3. Test Clear EXIF (Default download)
        var downloadResponse = await _http.GetAsync(uploadResult.InternetPath);
        downloadResponse.EnsureSuccessStatusCode();
        Assert.AreEqual("image/gif", downloadResponse.Content.Headers.ContentType?.MediaType);

        // 4. Test Compression
        var compressedResponse = await _http.GetAsync(uploadResult.InternetPath + "?w=100");
        compressedResponse.EnsureSuccessStatusCode();
        Assert.AreEqual("image/gif", compressedResponse.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task AvatarPngCompressionTest()
    {
        // 1. Register and Login
        await RegisterAndLoginAsync();

        // 2. Upload a PNG file
        // 1x2 PNG
        var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAACCAIAAAAW4yFwAAAAEElEQVR4nGP4z8DAxMDAAAAHCQEClNBcOwAAAABJRU5ErkJggg==");
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(fileContent, "file", "avatar.png");

        var uploadResponse = await _http.PostAsync("/upload/avatars", multipartContent);
        uploadResponse.EnsureSuccessStatusCode();

        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();
        Assert.IsNotNull(uploadResult);
        Assert.IsNotNull(uploadResult.InternetPath);

        // 3. Test Compression
        var compressedResponse = await _http.GetAsync(uploadResult.InternetPath + "?w=100");
        compressedResponse.EnsureSuccessStatusCode();

        // Verify it is an image and likely PNG
        Assert.AreEqual("image/png", compressedResponse.Content.Headers.ContentType?.MediaType);

        // Verify dimensions
        await using var stream = await compressedResponse.Content.ReadAsStreamAsync();
        using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
        Assert.AreEqual(128, image.Width);
        Assert.AreEqual(256, image.Height);
    }

    [TestMethod]
    public async Task AvatarPngCompressionSquareTest()
    {
        // 1. Register and Login
        await RegisterAndLoginAsync();

        // 2. Upload a PNG file
        // 1x2 PNG
        var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAACCAIAAAAW4yFwAAAAEElEQVR4nGP4z8DAxMDAAAAHCQEClNBcOwAAAABJRU5ErkJggg==");
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(fileContent, "file", "avatar.png");

        var uploadResponse = await _http.PostAsync("/upload/avatars", multipartContent);
        uploadResponse.EnsureSuccessStatusCode();

        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();
        Assert.IsNotNull(uploadResult);
        Assert.IsNotNull(uploadResult.InternetPath);

        // 3. Test Compression
        var compressedResponse = await _http.GetAsync(uploadResult.InternetPath + "?w=100&square=true");
        compressedResponse.EnsureSuccessStatusCode();

        // Verify it is an image and likely PNG
        Assert.AreEqual("image/png", compressedResponse.Content.Headers.ContentType?.MediaType);

        // Verify dimensions
        await using var stream = await compressedResponse.Content.ReadAsStreamAsync();
        using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
        Assert.AreEqual(128, image.Width);
        Assert.AreEqual(128, image.Height);
    }

    private class UploadResult
    {
        public string Path { get; init; } = string.Empty;
        public string InternetPath { get; init; } = string.Empty;
    }
}
#pragma warning restore CS8602
