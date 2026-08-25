namespace Aiursoft.AiurDrive.Services.FileStorage;

public static class StorageServiceAiurDriveExtensions
{
    public static void CreateDirectory(this StorageService storage, string logicalPath, bool isVault = false)
    {
        Directory.CreateDirectory(storage.GetFilePhysicalPath(logicalPath, isVault));
    }

    public static void DeleteFileOrDirectory(this StorageService storage, string logicalPath, bool isVault = false)
    {
        var physicalPath = storage.GetFilePhysicalPath(logicalPath, isVault);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
        else if (Directory.Exists(physicalPath))
        {
            Directory.Delete(physicalPath, recursive: true);
        }
    }

    public static void RenameFileOrDirectory(
        this StorageService storage,
        string logicalPath,
        string newName,
        bool isVault = false)
    {
        var sourcePath = storage.GetFilePhysicalPath(logicalPath, isVault);
        var parentPath = Path.GetDirectoryName(logicalPath);
        var destinationPath = storage.GetFilePhysicalPath(
            Path.Combine(parentPath ?? string.Empty, newName),
            isVault);
        Move(sourcePath, destinationPath);
    }

    public static void MoveFileOrDirectory(
        this StorageService storage,
        string sourceLogicalPath,
        string targetLogicalPath,
        bool isVault = false)
    {
        var sourcePath = storage.GetFilePhysicalPath(sourceLogicalPath, isVault);
        var destinationPath = storage.GetFilePhysicalPath(
            Path.Combine(targetLogicalPath, Path.GetFileName(sourceLogicalPath)),
            isVault);
        Move(sourcePath, destinationPath);
    }

    public static void DeleteSiteFolder(this StorageService storage, string siteName)
    {
        storage.DeleteFileOrDirectory(siteName, isVault: false);
        storage.DeleteFileOrDirectory(siteName, isVault: true);
    }

    public static void RenameSiteFolder(this StorageService storage, string oldSiteName, string newSiteName)
    {
        RenameSiteFolder(storage, oldSiteName, newSiteName, isVault: false);
        RenameSiteFolder(storage, oldSiteName, newSiteName, isVault: true);
    }

    public static long GetSiteSize(this StorageService storage, string siteName)
    {
        return GetDirectorySize(storage.GetFilePhysicalPath(siteName, isVault: false)) +
               GetDirectorySize(storage.GetFilePhysicalPath(siteName, isVault: true));
    }

    private static void Move(string sourcePath, string destinationPath)
    {
        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
        {
            throw new InvalidOperationException("The destination already exists.");
        }

        if (File.Exists(sourcePath))
        {
            File.Move(sourcePath, destinationPath);
        }
        else if (Directory.Exists(sourcePath))
        {
            Directory.Move(sourcePath, destinationPath);
        }
        else
        {
            throw new FileNotFoundException("The source does not exist.", sourcePath);
        }
    }

    private static void RenameSiteFolder(
        StorageService storage,
        string oldSiteName,
        string newSiteName,
        bool isVault)
    {
        var sourcePath = storage.GetFilePhysicalPath(oldSiteName, isVault);
        if (!Directory.Exists(sourcePath))
        {
            return;
        }

        var destinationPath = storage.GetFilePhysicalPath(newSiteName, isVault);
        Move(sourcePath, destinationPath);
    }

    private static long GetDirectorySize(string path)
    {
        return Directory.Exists(path)
            ? new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length)
            : 0;
    }
}
