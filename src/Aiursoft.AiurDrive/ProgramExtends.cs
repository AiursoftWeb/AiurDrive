using System.Diagnostics.CodeAnalysis;
using Aiursoft.Canon;
using Aiursoft.CSTools.Services;
using Microsoft.Extensions.Options;

namespace Aiursoft.AiurDrive;

public static class ProgramExtends
{
    [ExcludeFromCodeCoverage]
    public static async Task PullContainersAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var commandService = services.GetRequiredService<CommandService>();
        var logger = services.GetRequiredService<ILogger<Startup>>();
        var retryEngine = services.GetRequiredService<RetryEngine>();
        var aiFeatures = services.GetRequiredService<IOptions<AiFeaturesSettings>>();

        if (aiFeatures.Value.HyperScaling)
        {
            var imageName = "hub.aiursoft.cn/aiursoft/internalimages/swinir";
            await retryEngine.RunWithRetry(async _ =>
            {
                logger.LogInformation("Pulling docker image {Image}", imageName);
                var result = await commandService.RunCommandAsync("docker", $"pull {imageName}",
                    path: Path.GetTempPath(), timeout: TimeSpan.FromMinutes(5));
                if (result.code != 0)
                {
                    throw new Exception($"Failed to pull docker image {imageName}! Error: {result.error}");
                }
            }, 5);
        }
    }
}
