namespace Aiursoft.AiurDrive.Services;

public static class Mime
{
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "txt", "text/plain" },
        { "pdf", "application/pdf" },
        { "jpg", "image/jpeg" },
        { "jpeg", "image/jpeg" },
        { "png", "image/png" },
        { "gif", "image/gif" },
        { "bmp", "image/bmp" },
        { "ico", "image/x-icon" },
        { "svg", "image/svg+xml" },
        { "webp", "image/webp" },
        { "zip", "application/zip" },
        { "tar", "application/x-tar" },
        { "gz", "application/gzip" },
        { "rar", "application/vnd.rar" },
        { "7z", "application/x-7z-compressed" }, 
        { "mp3", "audio/mpeg" },
        { "wav", "audio/wav" },
        { "ogg", "audio/ogg" },
        { "mp4", "video/mp4" },
        { "mkv", "video/x-matroska" },
        { "avi", "video/x-msvideo" },
        { "mov", "video/quicktime" },
        { "doc", "application/msword" },
        { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { "xls", "application/vnd.ms-excel" },
        { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { "ppt", "application/vnd.ms-powerpoint" },
        { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { "js", "text/javascript" },
        { "ts", "text/typescript" },
        { "cs", "text/x-csharp" },
        { "html", "text/html" },
        { "htm", "text/html" },
        { "css", "text/css" },
        { "md", "text/markdown" },
        { "sql", "application/sql" },
        { "json", "application/json" },
        { "xml", "text/xml" },
        { "sh", "application/x-sh" },
        { "py", "text/x-python" },
        { "php", "text/x-php" },
        { "cpp", "text/x-c++src" },
        { "c", "text/x-csrc" },
        { "h", "text/x-chdr" },
        { "java", "text/x-java-source" },
        { "go", "text/x-go" },
        { "rs", "text/rust" },
        { "yml", "application/x-yaml" },
        { "yaml", "application/x-yaml" },
        { "apk", "application/vnd.android.package-archive" },
        { "exe", "application/x-msdownload" },
        { "msi", "application/x-msdownload" },
        { "deb", "application/vnd.debian.binary-package" },
        { "rpm", "application/x-rpm" },
        { "iso", "application/x-cd-image" },
        { "epub", "application/epub+zip" },
        { "torrent", "application/x-bittorrent" },
        { "kt", "text/x-kotlin" },
        { "scala", "text/x-scala" },
        { "lua", "text/x-lua" },
        { "toml", "application/toml" },
        { "dockerfile", "text/x-dockerfile" }
    };

    public static string GetContentType(string extension)
    {
        extension = extension.TrimStart('.');
        return MimeTypes.TryGetValue(extension, out var contentType) ? contentType : "application/octet-stream";
    }
}