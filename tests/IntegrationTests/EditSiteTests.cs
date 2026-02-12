using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System;

namespace Aiursoft.AiurDrive.Tests.IntegrationTests;

[TestClass]
public class EditSiteTests : TestBase
{
    [TestMethod]
    public async Task EditSiteAndRenameTest()
    {
        // Step 1: Register and login
        await RegisterAndLoginAsync();

        // Step 2: Create a site
        var siteName = $"test-site-{Guid.NewGuid().ToString().Substring(0, 8)}";
        var createResponse = await PostForm("/Dashboard/CreateSite", new Dictionary<string, string>
        {
            { "SiteName", siteName },
            { "AllowAnonymousView", "false" }
        });
        Assert.AreEqual(HttpStatusCode.Found, createResponse.StatusCode);

        // Step 2.5: Verify EditSite page is accessible
        var editPageGetResponse = await Http.GetAsync($"/Dashboard/EditSite/{siteName}");
        Assert.AreEqual(HttpStatusCode.OK, editPageGetResponse.StatusCode);

        // Step 3: Rename the site
        var newSiteName = $"renamed-site-{Guid.NewGuid().ToString().Substring(0, 8)}";
        var editResponse = await PostForm($"/Dashboard/EditSite/{siteName}", new Dictionary<string, string>
        {
            { "SiteName", newSiteName },
            { "Description", "New Description" },
            { "AllowAnonymousView", "true" }
        });

        // Step 4: Assert redirect to the new Files page
        AssertRedirect(editResponse, $"/Dashboard/Files/{newSiteName}");

        // Step 5: Verify the new Files page is accessible
        var filesPageResponse = await Http.GetAsync($"/Dashboard/Files/{newSiteName}");
        filesPageResponse.EnsureSuccessStatusCode();
        var html = await filesPageResponse.Content.ReadAsStringAsync();
        Assert.Contains(newSiteName, html);

        // Step 6: Verify the old Files page is NOT accessible (should be 404)
        var oldFilesPageResponse = await Http.GetAsync($"/Dashboard/Files/{siteName}");
        Assert.AreEqual(HttpStatusCode.NotFound, oldFilesPageResponse.StatusCode);
    }
}
