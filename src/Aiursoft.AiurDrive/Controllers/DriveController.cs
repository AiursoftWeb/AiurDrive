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

        var (folders, files) = fsService.ListDirectory(user.Email!, path);
        var usedStorage = fsService.CalculateStorageUsage(user.Email!);

        var model = new DriveIndexViewModel
        {
            CurrentPath = path,
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
            fsService.CreateFolder(user.Email!, parentPath ?? string.Empty, name);
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
            fsService.Rename(user.Email!, path, newName);
            
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
            fsService.Delete(user.Email!, path);
            
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
            // Source file from FilesController upload
            var sourcePath = Path.Combine("/tmp/data", filePath);
            if (!System.IO.File.Exists(sourcePath))
            {
                TempData["Error"] = "Uploaded file not found.";
                return RedirectToAction(nameof(Index), new { path = currentPath });
            }

            // Target directory in user's drive
            var userRoot = fsService.GetUserRootPath(user.Email!);
            var targetDir = string.IsNullOrEmpty(currentPath) 
                ? userRoot 
                : Path.Combine(userRoot, currentPath);
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

    [HttpGet("Download/{*path}")]
    public async Task<IActionResult> Download(string path)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        try
        {
            var userRoot = fsService.GetUserRootPath(user.Email!);
            var filePath = Path.Combine(userRoot, path);

            // Security check: ensure file is within user's directory
            var normalizedPath = Path.GetFullPath(filePath);
            if (!normalizedPath.StartsWith(userRoot))
            {
                return Forbid();
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var fileName = Path.GetFileName(filePath);
            var contentType = "application/octet-stream";
            
            return PhysicalFile(filePath, contentType, fileName);
        }
        catch (Exception)
        {
            return NotFound();
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

        var results = fsService.SearchFiles(user.Email!, query);
        var usedStorage = fsService.CalculateStorageUsage(user.Email!);

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
