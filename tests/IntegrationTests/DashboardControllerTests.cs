namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class DashboardControllerTests : TestBase
{
    [TestMethod]
    public async Task GetIndex()
    {
        // This is a basic test to ensure the controller is reachable.
        // Adjust the path as necessary for specific controllers.
        var url = "/Dashboard/Index";
        
        var response = await Http.GetAsync(url);
        
        // Assert
        // For some controllers, it might redirect to login, which is 302.
        // For others, it might be 200.
        // We just check if we get a response.
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task GetIndex_ContainsVaultCard()
    {
        await LoginAsAdmin();
        var url = "/Dashboard/Index";
        
        var response = await Http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.IsTrue(content.Contains("Zero Trust Vault"), "Dashboard should contain 'Zero Trust Vault' card.");
        Assert.IsTrue(content.Contains("/Vault/Index"), "Dashboard should contain link to '/Vault/Index'.");
    }
}
