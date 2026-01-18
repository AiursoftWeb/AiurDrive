using Aiursoft.AiurDrive.Configuration;
using Aiursoft.AiurDrive.Services;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class ImagePreviewTests : TestBase
{
    [TestMethod]
    public async Task TestImagePreviewSetting()
    {
        // 1. Register and Login
        await RegisterAndLoginAsync();

        // 2. Create a site
        var siteName = "previewtestsite";
        await PostForm("/Dashboard/CreateSite", new Dictionary<string, string>
        {
            { "SiteName", siteName },
            { "OpenToUpload", "true" }
        });

        // 3. Upload a PNG file
        // 1x2 PNG
        var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAACCAIAAAAW4yFwAAAAEElEQVR4nGP4z8DAxMDAAAAHCQEClNBcOwAAAABJRU5ErkJggg==");
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(fileContent, "file", "test.png");

        var uploadResponse = await Http.PostAsync($"/upload/{siteName}", multipartContent);
        uploadResponse.EnsureSuccessStatusCode();
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();
        Assert.IsNotNull(uploadResult);


        // 4. Default state: AllowImagePreview is True
        // The file was uploaded to a path like "previewtestsite/2026/01/18/test.png"
        // Note: StorageService may rename the file if there's a collision (adds _ prefix)
        var uploadedPath = uploadResult.Path; // Use the actual path returned
        var subdirectory = Path.GetDirectoryName(uploadedPath)?.Replace("\\", "/").Replace($"{siteName}/", ""); // e.g., "2026/01/18"
        
        // Verify view has <img> tag in the subdirectory where the file was uploaded
        var filesViewResponse = await Http.GetAsync($"/Dashboard/Files/{siteName}/{subdirectory}");
        var filesViewHtml = await filesViewResponse.Content.ReadAsStringAsync();
        
        var expectedThumbPath = $"<img src=\"/download/{uploadedPath}?w=200\"";
        Assert.Contains(expectedThumbPath, filesViewHtml);

        // Verify controller returns compressed image
        var compressedResponse = await Http.GetAsync(uploadResult.InternetPath + "?w=100");
        compressedResponse.EnsureSuccessStatusCode();
        await using (var stream = await compressedResponse.Content.ReadAsStreamAsync())
        {
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
            // Default compression ceiling for 100 is 128
            Assert.AreEqual(128, image.Width);
        }

        // 5. Disable AllowImagePreview
        using (var scope = Server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            await settingsService.UpdateSettingAsync(SettingsMap.AllowImagePreview, "False");
        }


        // 6. Verify view does NOT have <img> tag for the image, instead uses icon
        filesViewResponse = await Http.GetAsync($"/Dashboard/Files/{siteName}/{subdirectory}");
        filesViewHtml = await filesViewResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(expectedThumbPath, filesViewHtml);
        Assert.Contains("/images/mimetypes/image-x-generic.svg", filesViewHtml);

        // 7. Verify controller returns ORIGINAL image even with ?w=100
        var rawResponse = await Http.GetAsync(uploadResult.InternetPath + "?w=100");
        rawResponse.EnsureSuccessStatusCode();
        await using (var stream = await rawResponse.Content.ReadAsStreamAsync())
        {
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
            // Original width was 1
            Assert.AreEqual(1, image.Width);
        }
    }

    private class UploadResult
    {
        public string Path { get; init; } = string.Empty;
        public string InternetPath { get; init; } = string.Empty;
    }
}
