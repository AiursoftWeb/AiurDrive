using Aiursoft.Canon;
using Aiursoft.CSTools.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Aiursoft.AiurDrive.Services;

public class UpScaleService(
    CanonQueue queue,
    ILogger<UpScaleService> logger,
    CommandService commandService)
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "aiurdrive-tmp", "hyper-scale");
    
    public async Task<bool> IsSupportedImageFileAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".bmp") return false;
        if (File.Exists(filePath) == false) return false;
        
        // If more than 1.5MB, return false.
        //if (new FileInfo(filePath).Length > 1.5 * 1024 * 1024) return false;
        
        try
        {
            using var image = await Image.LoadAsync(filePath);
            return true;
        }
        catch (UnknownImageFormatException e)
        {
            logger.LogError(e, "Failed to load image file {FilePath}", filePath);
        }

        return false;
    }
    
    public async Task<string> UpScaleImage(string inputImage, string outputPath)
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

        // Run the up-scale image.
        try
        {
            var command =
                $"run --rm --name {buildId} --gpus all --cpus=16 --memory=4096m -v {buildInputFolder}:/app/input -v {buildOutputFolder}:/app/results/swinir_real_sr_x4_large hub.aiursoft.cn/aiursoft/internalimages/swinir";
            var (resultCode, output, error) = await commandService.RunCommandAsync(
                bin: "docker",
                arg: command,
                path: _tempFolder,
                timeout: TimeSpan.FromSeconds(300),
                killTimeoutProcess: true);

            if (resultCode != 0)
            {
                var message = $"Failed to run up-scale image: {output}, {error}";
                logger.LogError("Failed to run up-scale image: {Out}, {Error}", output, error);
                throw new InvalidOperationException(message);
            }

            if (!Directory.EnumerateFiles(buildOutputFolder).Any())
            {
                const string message = "No output file generated!";
                logger.LogError("No output file generated!");
                throw new InvalidOperationException(message);
            }

            // Convert the output image to JPG format.
            var outputImage = Directory.EnumerateFiles(buildOutputFolder).First();
            outputImage = await ConvertPhotoToJpg(outputImage, buildOutputFolder);
            
            // Copy the output image to the output folder.
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

    private async Task<string> ConvertPhotoToJpg(string inputImage, string outputPath)
    {
        if (Path.GetExtension(inputImage) == ".jpg")
        {
            return inputImage;
        }
        
        var jpgFileOutputPath = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(inputImage) + ".jpg");
        using Image image = await Image.LoadAsync(inputImage);
        // 将图片保存为 JPG 格式
        await image.SaveAsync(jpgFileOutputPath, new JpegEncoder());
        return jpgFileOutputPath;
    }
}