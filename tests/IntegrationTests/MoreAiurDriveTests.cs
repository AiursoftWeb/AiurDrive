using System.Net;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class MoreAiurDriveTests : TestBase
{
    [TestMethod]
    public async Task TestSharedWithMe()
    {
        await RegisterAndLoginAsync();
        var response = await Http.GetAsync("/Shares/SharedWithMe");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task TestSearchUsers()
    {
        await LoginAsAdmin();
        var response = await Http.GetAsync("/Users/Search?query=admin");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("admin"));
    }

    [TestMethod]
    public async Task TestProgramExtends()
    {
        await Server!.CopyAvatarFileAsync();
        await Server!.CopyAvatarFileAsync();
    }
}
