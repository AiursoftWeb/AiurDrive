using System.Security.Cryptography;
using System.Text;
using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.AiurDrive.Services;

public class DriveStorageQuota(StorageRootPathProvider root) : IScopedDependency
{
    public const long GiB = 1024L * 1024 * 1024;

    public async Task<FileStream> LockAsync(string scope, CancellationToken cancellationToken)
    {
        // An OS-held lock also serializes writers in other app processes sharing this volume.
        // Keep the lock file: unlinking it could give concurrent writers different lock inodes.
        var folder = Path.Combine(root.GetStorageRootPath(), ".quota-locks");
        Directory.CreateDirectory(folder);
        var name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope)));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(Path.Combine(folder, name), FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException exception) when ((exception.HResult & 0xffff) is 11 or 32 or 33)
            {
                await Task.Delay(50, cancellationToken);
            }
        }
    }

    public void Check(long limitBytes, long additionalBytes, params string[] folders)
    {
        // Permit deletion and shrinking even when an existing account is over its new quota.
        if (additionalBytes <= 0) return;
        var used = folders.Sum(GetSize);
        if (used > limitBytes || additionalBytes > limitBytes - used)
        {
            throw new StorageQuotaExceededException();
        }
    }

    private static long GetSize(string folder)
    {
        if (!Directory.Exists(folder)) return 0;
        return Directory.EnumerateFiles(folder, "*", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = false,
            AttributesToSkip = FileAttributes.ReparsePoint
        }).Sum(file => new FileInfo(file).Length);
    }
}

public class StorageQuotaExceededException : Exception;
