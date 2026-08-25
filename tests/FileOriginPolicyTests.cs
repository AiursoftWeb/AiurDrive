using Aiursoft.AiurDrive.Services.FileStorage;

namespace Aiursoft.AiurDrive.Tests;

[TestClass]
public class FileOriginPolicyTests
{
    [TestMethod]
    public void InlineRenderingRequiresTheConfiguredFileOrigin()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:PublicOrigin"] = "https://files.example.com",
                ["Storage:RequireDedicatedInlineOrigin"] = "true"
            })
            .Build();
        var policy = new FileDeliveryPolicy(configuration);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("app.example.com");

        Assert.IsFalse(policy.CanRenderInline(context.Request));

        context.Request.Host = new HostString("files.example.com");
        Assert.IsTrue(policy.CanRenderInline(context.Request));
    }
}
