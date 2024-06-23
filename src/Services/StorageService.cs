namespace Aiursoft.AiurDrive.Services;

public class StorageService(IConfiguration configuration)
{
    private readonly string _workspaceFolder = configuration["Storage:Path"]!;
    
    public async Task Save(string savePath, IFormFile file)
    {
        var finalFilePath = Path.Combine(_workspaceFolder, savePath);
        var finalFolder = Path.GetDirectoryName(finalFilePath);
        //Try saving file.
        if (!Directory.Exists(finalFolder))
        {
            Directory.CreateDirectory(finalFolder!);
        }

        await using var fileStream = new FileStream(finalFilePath, FileMode.Create);
        await file.CopyToAsync(fileStream);
        fileStream.Close();
    }
    
    public string GetFilePhysicalPath(string fileName)
    {
        return Path.Combine(_workspaceFolder, fileName);
    }
}