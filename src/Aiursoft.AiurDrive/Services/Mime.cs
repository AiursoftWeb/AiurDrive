namespace Aiursoft.AiurDrive.Services;

public static class Mime
{
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "txt", "text/x-generic" },
        { "pdf", "application/pdf" },
        { "jpg", "application/image-jpg" },
        { "jpeg", "application/image-jpg" },
        { "png", "image/x-generic" },
        { "gif", "application/image-gif" },
        { "bmp", "application/image-bmp" },
        { "ico", "application/image-ico" },
        { "svg", "image/svg+xml" },
        { "webp", "image/webp" },
        { "zip", "application/x-zip" },
        { "tar", "application/x-tar" },
        { "gz", "application/x-gzip" },
        { "rar", "application/x-rar" },
        { "7z", "application/x-zip" }, 
        { "mp3", "audio/x-generic" },
        { "wav", "audio/x-generic" },
        { "ogg", "application/ogg" },
        { "mp4", "video/x-generic" },
        { "mkv", "video/x-generic" },
        { "avi", "video/x-generic" },
        { "mov", "video/x-generic" },
        { "doc", "application/vnd.ms-word" },
        { "docx", "application/vnd.ms-word" },
        { "xls", "application/vnd.ms-excel" },
        { "xlsx", "application/vnd.ms-excel" },
        { "ppt", "application/vnd.ms-powerpoint" },
        { "pptx", "application/vnd.ms-powerpoint" },
        { "js", "text/x-javascript" },
        { "ts", "text/x-typescript" },
        { "cs", "text/x-csharp" },
        { "html", "text/html" },
        { "htm", "text/html" },
        { "css", "text/css" },
        { "md", "text/markdown" },
        { "sql", "application/sql" },
        { "json", "application/json" },
        { "xml", "text/xml" },
        { "sh", "application/x-shellscript" },
        { "py", "text/x-python" },
        { "php", "text/x-php" },
        { "cpp", "text/x-cpp" },
        { "c", "text/x-c" },
        { "h", "text/x-chdr" },
        { "java", "text/x-java" },
        { "go", "text/x-go" },
        { "rs", "text/rust" },
        { "yml", "application/x-yaml" },
        { "yaml", "application/x-yaml" },
        { "apk", "application/apk" },
        { "exe", "application/x-ms-dos-executable" },
        { "msi", "application/x-ms-dos-executable" },
        { "deb", "application/x-deb" },
        { "rpm", "application/x-rpm" },
        { "iso", "application/x-cd-image" },
        { "epub", "application/epub+zip" },
        { "torrent", "application/x-bittorrent" },
        { "kt", "text/x-kotlin" },
        { "scala", "text/x-scala" },
        { "lua", "text/x-lua" },
        { "toml", "application/toml" },
        { "dockerfile", "text/dockerfile" }
    };

    public static string GetContentType(string extension)
    {
        extension = extension.TrimStart('.');
        return MimeTypes.TryGetValue(extension, out var contentType) ? contentType : "application/octet-stream";
    }
}