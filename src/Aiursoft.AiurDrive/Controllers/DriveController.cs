using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Models.DriveViewModels;
using Aiursoft.AiurDrive.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

[Authorize]
[LimitPerMin]
public class DriveController(
    DriveFileSystemService fsService,
    UserManager<User> userManager) : Controller
{
    [HttpGet]
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "My Drive",
        CascadedLinksIcon = "hard-drive",
        CascadedLinksOrder = 1,
        LinkText = "File Manager",
        LinkOrder = 1)]
    public async Task<IActionResult> Index(string? path)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var (folders, files) = fsService.ListDirectory(user.Id, path);
        var usedStorage = fsService.CalculateStorageUsage(user.Id);

        // Use user ID for storage path (clean UUID-based structure)
        var userStoragePath = $"drive/{user.Id}";

        var model = new DriveIndexViewModel
        {
            CurrentPath = path,
            UserStoragePath = userStoragePath,
            Folders = folders,
            Files = files,
            UsedStorageBytes = usedStorage,
            TotalStorageBytes = user.TotalStorageBytes
        };

        return this.StackView(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder(string? parentPath, string name)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Folder name is required.");
        }

        try
        {
            fsService.CreateFolder(user.Id, parentPath ?? string.Empty, name);
            return RedirectToAction(nameof(Index), new { path = parentPath });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { path = parentPath });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Rename(string path, string newName)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            return BadRequest("Name is required.");
        }

        try
        {
            fsService.Rename(user.Id, path, newName);
            
            // Return to parent directory
            var parentPath = Path.GetDirectoryName(path);
            return RedirectToAction(nameof(Index), new { path = parentPath });
        }
        catch (FileNotFoundException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string path)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        try
        {
            fsService.Delete(user.Id, path);
            
            // Return to parent directory
            var parentPath = Path.GetDirectoryName(path);
            return RedirectToAction(nameof(Index), new { path = parentPath });
        }
        catch (FileNotFoundException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveUploadedFile(string filePath, string? currentPath, string originalFileName)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        try
        {
            // SECURITY: Validate filePath to prevent path traversal
            // Source file must be within /tmp/data directory (FilesController upload area)
            var dataRoot = "/tmp/data";
            var sourcePath = Path.Combine(dataRoot, filePath);
            var normalizedSource = Path.GetFullPath(sourcePath);
            
            if (!normalizedSource.StartsWith(Path.GetFullPath(dataRoot)))
            {
                TempData["Error"] = "Invalid file path.";
                return RedirectToAction(nameof(Index), new { path = currentPath });
            }
            
            if (!System.IO.File.Exists(sourcePath))
            {
                TempData["Error"] = "Uploaded file not found.";
                return RedirectToAction(nameof(Index), new { path = currentPath });
            }

            // SECURITY: Target directory uses fsService which has built-in path validation
            var userRoot = fsService.GetUserRootPath(user.Id);
            var targetDir = string.IsNullOrEmpty(currentPath) 
                ? userRoot 
                : Path.Combine(userRoot, currentPath);
            
            // Ensure target is within user's directory
            var normalizedTarget = Path.GetFullPath(targetDir);
            if (!normalizedTarget.StartsWith(userRoot))
            {
                TempData["Error"] = "Invalid target path.";
                return RedirectToAction(nameof(Index), new { path = currentPath });
            }
            
            Directory.CreateDirectory(targetDir);

            // Use original filename instead of the one from path (which may have underscores)
            var targetPath = Path.Combine(targetDir, originalFileName);
            System.IO.File.Move(sourcePath, targetPath, overwrite: true);

            TempData["Success"] = $"File '{originalFileName}' uploaded successfully.";
            return RedirectToAction(nameof(Index), new { path = currentPath });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to save file: {ex.Message}";
            return RedirectToAction(nameof(Index), new { path = currentPath });
        }
    }



    [HttpGet]
    public async Task<IActionResult> Search(string query)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return RedirectToAction(nameof(Index));
        }

        var results = fsService.SearchFiles(user.Id, query);
        var usedStorage = fsService.CalculateStorageUsage(user.Id);

        var model = new DriveIndexViewModel
        {
            CurrentPath = $"Search: {query}",
            Folders = Array.Empty<DirectoryInfo>(),
            Files = results,
            UsedStorageBytes = usedStorage,
            TotalStorageBytes = user.TotalStorageBytes
        };

        return this.StackView(model);
    }
}
