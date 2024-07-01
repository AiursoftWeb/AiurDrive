﻿namespace Aiursoft.AiurDrive.Services;

/// <summary>
/// Represents a service for storing and retrieving files.
/// </summary>
public class StorageService(IConfiguration configuration)
{
    public readonly string WorkspaceFolder = Path.Combine(configuration["Storage:Path"]!, "Workspace");
    
    // Async lock.
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Saves a file to the storage.
    /// </summary>
    /// <param name="savePath">The path where the file will be saved. The 'savePath' is the path that the user wants to save. Not related to actual disk path.</param>
    /// <param name="file">The file to be saved.</param>
    /// <returns>The actual path where the file is saved relative to the workspace folder.</returns>
    public async Task<string> Save(string savePath, IFormFile file)
    {
        var finalFilePath = Path.Combine(WorkspaceFolder, savePath);
        var finalFolder = Path.GetDirectoryName(finalFilePath);

        // Create the folder if it does not exist.
        if (!Directory.Exists(finalFolder))
        {
            Directory.CreateDirectory(finalFolder!);
        }

        // The problem is: What if the file already exists?
        await _lock.WaitAsync();
        try
        {
            var expectedFileName = Path.GetFileName(finalFilePath);
            while (File.Exists(finalFilePath))
            {
                expectedFileName = "_" + expectedFileName;
                finalFilePath = Path.Combine(finalFolder!, expectedFileName);
            }

            // Create a new file.
            File.Create(finalFilePath).Close();
        }
        finally
        {
            _lock.Release();
        }
        
        await using var fileStream = new FileStream(finalFilePath, FileMode.Create);
        await file.CopyToAsync(fileStream);
        fileStream.Close();
        
        return Path.GetRelativePath(WorkspaceFolder, finalFilePath);
    }
    
    public string GetFilePhysicalPath(string fileName)
    {
        return Path.Combine(WorkspaceFolder, fileName);
    }
    
    public string AbsolutePathToRelativePath(string absolutePath)
    {
        return Path.GetRelativePath(WorkspaceFolder, absolutePath);
    }

    public string RelativePathToUriPath(string relativePath)
    {
        var urlPath = Uri.EscapeDataString(relativePath)
            .Replace("%5C", "/")
            .Replace("%5c", "/")
            .Replace("%2F", "/")
            .Replace("%2f", "/")
            .TrimStart('/');
        return urlPath;
    }
}