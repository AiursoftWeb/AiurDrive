using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class VaultIntegrationTests : TestBase
{
    [TestMethod]
    public async Task TestVaultPageRendersSuccessfully()
    {
        // 1. Register and login
        await RegisterAndLoginAsync();

        // 2. Request Vault Index
        var response = await Http.GetAsync("/Vault/Index");

        // 3. Verify status code is 200
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Vault page failed to render with 200 OK.");

        // 4. Verify content contains our JS logic and vault markers
        var html = await response.Content.ReadAsStringAsync();
        
        // Ensure the vault containers are present
        Assert.IsTrue(html.Contains("id=\"vault-loading\""), "Vault loading container is missing.");
        Assert.IsTrue(html.Contains("id=\"vault-init\""), "Vault init container is missing.");
        Assert.IsTrue(html.Contains("id=\"vault-login\""), "Vault login container is missing.");
        
        // Ensure the title we set is in the HTML
        Assert.IsTrue(html.Contains("Zero-Trust Vault"), "The page title 'Zero-Trust Vault' is missing.");
        
        // Ensure the layout didn't crash
        Assert.IsTrue(html.Contains("My Drive"), "The layout seems to have crashed.");

        // 5. Verify API endpoints are reachable
        var configResponse = await Http.GetAsync("/VaultApi/GetConfig");
        // Should be 404 since it's a new user
        Assert.AreEqual(HttpStatusCode.NotFound, configResponse.StatusCode);

        // 6. Test Init API (Fixes the 500 error reported)
        var initContent = new
        {
            VaultSaltBase64 = "c2FsdA==", // "salt"
            VerifierNonceBase64 = "bm9uY2U=",
            VerifierTagBase64 = "dGFn",
            VerifierCipherBase64 = "Y2lwaGVy"
        };
        var initResponse = await Http.PostAsJsonAsync("/VaultApi/Init", initContent);
        Assert.AreEqual(HttpStatusCode.OK, initResponse.StatusCode, "Init API failed.");

        // 7. Test GetConfig again
        var configAfterInitResponse = await Http.GetAsync("/VaultApi/GetConfig");
        Assert.AreEqual(HttpStatusCode.OK, configAfterInitResponse.StatusCode);
        var configJson = await configAfterInitResponse.Content.ReadAsStringAsync();
        Assert.IsTrue(configJson.Contains("VaultSaltBase64"), "Config content is invalid.");

        // 8. Test Upload API
        var fileContent = new ByteArrayContent("fake-encrypted-data"u8.ToArray());
        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "testuuid.enc");
        var uploadResponse = await Http.PostAsync("/VaultApi/Upload", form);
        Assert.AreEqual(HttpStatusCode.OK, uploadResponse.StatusCode, "Upload API failed.");

        // 9. Test List API
        var listResponse = await Http.GetAsync("/VaultApi/List");
        Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await listResponse.Content.ReadAsStringAsync();
        Assert.IsTrue(listJson.Contains("testuuid"), "Uploaded file missing from list.");

        // 10. Test Reset API
        var resetResponse = await Http.PostAsync("/VaultApi/Reset", null);
        Assert.AreEqual(HttpStatusCode.OK, resetResponse.StatusCode);

        // 11. Verify it's back to factory state
        var configAfterResetResponse = await Http.GetAsync("/VaultApi/GetConfig");
        Assert.AreEqual(HttpStatusCode.NotFound, configAfterResetResponse.StatusCode, "Config should be gone after reset.");
    }

    [TestMethod]
    public async Task TestVaultPageRequiresLogin()
    {
        // Request without login
        var response = await Http.GetAsync("/Vault/Index");
        
        // Should redirect to login
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.IsTrue(response.Headers.Location?.OriginalString.Contains("/Account/Login") == true);
    }
}
