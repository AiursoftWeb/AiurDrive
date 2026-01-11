using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.AiurDrive.Services;

/// <summary>
/// Core file system service for pure file-based drive operations.
/// Replaces database-backed metadata with direct file system access.
/// </summary>
public class DriveFileSystemService(
    StorageService storage,
    ILogger<DriveFileSystemService> logger) : IScopedDependency
{
    /// <summary>
    /// Get the root directory path for a user's drive.
    /// Structure: /data/drive/{userEmail}/
    /// </summary>
    public string GetUserRootPath(string userEmail)
    {
        var safeName = SanitizeEmail(userEmail);
        var rootPath = Path.Combine(storage.StorageRootFolder, "drive", safeName);
        
        // Ensure directory exists
        Directory.CreateDirectory(rootPath);
        
        return rootPath;
    }

    /// <summary>
    /// List all folders and files in a directory.
    /// </summary>
    public (DirectoryInfo[] folders, FileInfo[] files) ListDirectory(string userEmail, string? relativePath = null)
    {
        var fullPath = GetFullPath(userEmail, relativePath);
        
        if (!Directory.Exists(fullPath))
        {
            logger.LogWarning("Directory does not exist: {Path}", fullPath);
            return (Array.Empty<DirectoryInfo>(), Array.Empty<FileInfo>());
        }

        var dirInfo = new DirectoryInfo(fullPath);
        
        var folders = dirInfo.GetDirectories()
            .Where(d => !d.Name.StartsWith('.')) // Hide hidden folders
            .OrderBy(d => d.Name)
            .ToArray();
        
        var files = dirInfo.GetFiles()
            .Where(f => !f.Name.StartsWith('.')) // Hide hidden files
            .OrderBy(f => f.Name)
            .ToArray();

        return (folders, files);
    }

    /// <summary>
    /// Create a new folder.
    /// </summary>
    public void CreateFolder(string userEmail, string relativePath, string folderName)
    {
        var parentPath = GetFullPath(userEmail, relativePath);
        var newFolderPath = Path.Combine(parentPath, SanitizeFolderName(folderName));

        if (Directory.Exists(newFolderPath))
        {
            throw new InvalidOperationException($"Folder '{folderName}' already exists.");
        }

        Directory.CreateDirectory(newFolderPath);
        logger.LogInformation("Created folder: {Path}", newFolderPath);
    }

    /// <summary>
    /// Rename a file or folder.
    /// </summary>
    public void Rename(string userEmail, string relativePath, string newName)
    {
        var fullPath = GetFullPath(userEmail, relativePath);
        
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            throw new FileNotFoundException($"Path not found: {relativePath}");
        }

        var parentPath = Path.GetDirectoryName(fullPath)!;
        var newPath = Path.Combine(parentPath, SanitizeFolderName(newName));

        if (File.Exists(fullPath))
        {
            File.Move(fullPath, newPath);
        }
        else
        {
            Directory.Move(fullPath, newPath);
        }

        logger.LogInformation("Renamed {OldPath} to {NewPath}", fullPath, newPath);
    }

    /// <summary>
    /// Delete a file or folder (recursive for folders).
    /// </summary>
    public void Delete(string userEmail, string relativePath)
    {
        var fullPath = GetFullPath(userEmail, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("Deleted file: {Path}", fullPath);
        }
        else if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
            logger.LogInformation("Deleted folder: {Path}", fullPath);
        }
        else
        {
            throw new FileNotFoundException($"Path not found: {relativePath}");
        }
    }

    /// <summary>
    /// Calculate total storage usage for a user (recursive).
    /// </summary>
    public long CalculateStorageUsage(string userEmail)
    {
        var rootPath = GetUserRootPath(userEmail);
        return CalculateDirectorySize(rootPath);
    }

    /// <summary>
    /// Search for files by name pattern (recursive).
    /// </summary>
    public FileInfo[] SearchFiles(string userEmail, string searchPattern)
    {
        var rootPath = GetUserRootPath(userEmail);
        var dirInfo = new DirectoryInfo(rootPath);

        try
        {
            return dirInfo.EnumerateFiles($"*{searchPattern}*", SearchOption.AllDirectories)
                .Where(f => !f.Name.StartsWith('.'))
                .OrderByDescending(f => f.LastWriteTime)
                .Take(50) // Limit results
                .ToArray();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Access denied while searching in {Path}", rootPath);
            return Array.Empty<FileInfo>();
        }
    }

    /// <summary>
    /// Get the relative path from user root to a file/folder.
    /// </summary>
    public string GetRelativePath(string userEmail, string fullPath)
    {
        var rootPath = GetUserRootPath(userEmail);
        return Path.GetRelativePath(rootPath, fullPath);
    }

    // Private helper methods

    private string GetFullPath(string userEmail, string? relativePath)
    {
        var rootPath = GetUserRootPath(userEmail);
        
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return rootPath;
        }

        var fullPath = Path.Combine(rootPath, relativePath);
        
        // Security: Ensure path doesn't escape user's root
        var normalizedPath = Path.GetFullPath(fullPath);
        if (!normalizedPath.StartsWith(rootPath))
        {
            throw new UnauthorizedAccessException("Access denied: path escapes user root");
        }

        return normalizedPath;
    }

    private long CalculateDirectorySize(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        
        long size = 0;

        // Add file sizes
        foreach (var file in dirInfo.GetFiles())
        {
            if (!file.Name.StartsWith('.'))
            {
                size += file.Length;
            }
        }

        // Recursively add subdirectory sizes
        foreach (var dir in dirInfo.GetDirectories())
        {
            if (!dir.Name.StartsWith('.'))
            {
                size += CalculateDirectorySize(dir.FullName);
            }
        }

        return size;
    }

    private static string SanitizeEmail(string email)
    {
        // Replace @ and . with underscores for file system safety
        return email.Replace("@", "_at_").Replace(".", "_");
    }

    private static string SanitizeFolderName(string name)
    {
        // Remove invalid characters for Linux file systems
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
