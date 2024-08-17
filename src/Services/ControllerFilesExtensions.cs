﻿using Aiursoft.CSTools.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Services;

public static class ControllerFilesExtensions
{
    private static (string etag, long length) GetFileHttpProperties(string path)
    {
        var fileInfo = new FileInfo(path);
        var etagHash = fileInfo.LastWriteTime.ToUniversalTime().ToFileTime() ^ fileInfo.Length;
        var etag = Convert.ToString(etagHash, 16);
        return (etag, fileInfo.Length);
    }
    
    public static IActionResult WebFile(this ControllerBase controller, string path)
    {
        var (etag, length) = GetFileHttpProperties(path);

        // Handle etag
        controller.Response.Headers.Append("ETag", etag);
        if (controller.Request.Headers.Keys.Contains("If-None-Match"))
        {
            if (controller.Request.Headers["If-None-Match"].ToString().Trim('\"') == etag)
            {
                return new StatusCodeResult(304);
            }
        }

        var fileName = Path.GetFileName(path);
        var asciiFileName = Uri.EscapeDataString(fileName); // Ensuring ASCII encoding

        controller.Response.Headers.Append("Content-Disposition", $"inline; filename*=UTF-8''{asciiFileName}");
        controller.Response.Headers.Append("Content-Length", length.ToString());
        controller.Response.Headers.Append("Cache-Control", $"public, max-age={TimeSpan.FromDays(7).TotalSeconds}");

        var extension = Path.GetExtension(path).TrimStart('.');
        return controller.PhysicalFile(path, Mime.GetContentType(extension), true);
    }

}