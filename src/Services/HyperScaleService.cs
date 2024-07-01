using Aiursoft.Canon;
using Aiursoft.CSTools.Services;

namespace Aiursoft.AiurDrive.Services;

public class HyperScaleService(
    CanonQueue queue,
    ILogger<HyperScaleService> logger,
    CommandService commandService)
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "aiurdrive", "hyper-scale");
    
    public async Task<string> HyperScaleImage(string inputImage, string outputPath)
    {
        var buildId = Guid.NewGuid().ToString("N");
        var buildFolder = Path.Combine(_tempFolder, buildId);
        var buildInputFolder = Path.Combine(buildFolder, "input");
        var buildOutputFolder = Path.Combine(buildFolder, "output");
        Directory.CreateDirectory(buildFolder);
        Directory.CreateDirectory(buildInputFolder);
        Directory.CreateDirectory(buildOutputFolder);

        logger.LogInformation("Build ID: {BuildId}", buildId);
        // Copy the input image to the folder.
        var sourceFileName = Path.GetFileName(inputImage);
        File.Copy(inputImage, Path.Combine(buildInputFolder, sourceFileName));

        // Run the hyper-scale image.
        try
        {
            var command =
                $"run --gpus all --cpus=16 --memory=4096m -it -v {buildInputFolder}:/app/input -v {buildOutputFolder}:/app/results/swinir_real_sr_x4_large hub.aiursoft.cn/aiursoft/internalimages/swinir";
            var (resultCode, output, error) = await commandService.RunCommandAsync(
                bin: "docker",
                arg: command,
                path: _tempFolder,
                timeout: TimeSpan.FromSeconds(500),
                killTimeoutProcess: true);

            if (resultCode != 0)
            {
                var message = $"Failed to run hyper-scale image: {output}, {error}";
                logger.LogError("Failed to run hyper-scale image: {Out}, {Error}", output, error);
                throw new InvalidOperationException(message);
            }

            if (!Directory.EnumerateFiles(buildOutputFolder).Any())
            {
                const string message = "No output file generated!";
                logger.LogError("No output file generated!");
                throw new InvalidOperationException(message);
            }

            // Copy the output image to the output folder.
            var outputImage = Directory.EnumerateFiles(buildOutputFolder).First();
            var outputImageName = Path.GetFileName(outputImage);
            var finalOutputPath = Path.Combine(outputPath, outputImageName);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            if (File.Exists(finalOutputPath))
            {
                File.Delete(finalOutputPath);
            }
            File.Copy(outputImage, finalOutputPath);
            
            // Return the final output path.
            return finalOutputPath;
        }
        catch (TimeoutException e)
        {
            logger.LogError(e, "Timeout with build {Build}", buildId);
            throw;
        }
        finally
        {
            // Kill and remove the container.
            logger.LogInformation("Killing container {Build}", buildId);
            _ = await commandService.RunCommandAsync(
                bin: "docker",
                arg: $"kill {buildId}",
                path: _tempFolder,
                timeout: TimeSpan.FromSeconds(10));

            logger.LogInformation("Removing container {Build}", buildId);
            _ = await commandService.RunCommandAsync(
                bin: "docker",
                arg: $"rm -f {buildId}",
                path: _tempFolder,
                timeout: TimeSpan.FromSeconds(10));

            logger.LogInformation("Removing folder {Build}", buildId);
            queue.QueueNew(() =>
            {
                CSTools.Tools.FolderDeleter.DeleteByForce(buildFolder);
                return Task.CompletedTask;
            });
        }
    }
}