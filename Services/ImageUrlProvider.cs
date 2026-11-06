using System.Collections.Concurrent;

namespace BikeCarHub.Services;

/// <summary>Turns an image path stored in the database into a URL that will load. Swap this for Blob/CDN later.</summary>
public interface IImageUrlProvider
{
    string Placeholder { get; }
    /// <summary>Local URL when the file exists in the image root, otherwise the remote host, otherwise the placeholder.</summary>
    string Resolve(string? dbPath);
    /// <summary>Physical file for a /images/... path, or null when it is not in the local image root.</summary>
    string? MapToLocalFile(string? dbPath);
    /// <summary>Physical folder that holds the image, used to find an article's gallery and .docx.</summary>
    string? MapToLocalDirectory(string? dbPath);
    string? FindFolder(string virtualPrefix, string folderSlug);
    /// <summary>The .docx for an article's image folder: same sub-path under the private articles root, or null.</summary>
    string? FindArticleFile(string imageFolder);
    string ToVirtualPath(string physicalFile);
    /// <summary>Private folder that holds the article .docx files (Images:ArticlesRoot).</summary>
    string ArticlesRoot { get; }
}

public sealed class ImageUrlProvider : IImageUrlProvider
{
    // Database path prefix -> folder under the local image root (the wwwroot\images layout).
    private static readonly (string Prefix, string Folder)[] Map =
    [
        ("/images/twowheeler/brand logo/", "brands"),
        ("/images/twowheeler/twowheelerimages/", "bikes"),
        ("/images/two-wheeler/", "two-wheeler"),
        ("/images/blogs/", "blogs"),
        ("/images/latestnews/", "news"),
        ("/images/valueformoney/", "value-for-money"),
        ("/images/heroslides/", "hero"),
        ("/images/twowheeler/upcomming/", "upcoming"),
        ("/images/comparebikes/", "compare"),
        ("/images/upcoming/", "upcoming"),
        ("/images/compare/", "compare"),
        ("/images/bchlogo/", "site"),
        // Listed after bchlogo so ToVirtualPath keeps giving logo files their existing URLs.
        ("/images/site/", "site"),
        ("/images/default.webp", Path.Combine("site", "default.webp")),
        ("/images/twowheeler/default.webp", Path.Combine("site", "default.webp"))
    ];

    private readonly string _root;
    private readonly string _articlesRoot;
    private readonly string? _remoteBase;
    private readonly ConcurrentDictionary<string, string?> _fileCache = new(StringComparer.OrdinalIgnoreCase);

    public ImageUrlProvider(IConfiguration config, IWebHostEnvironment env)
    {
        var root = config["Images:LocalRoot"] ?? "wwwroot/images";
        _root = Path.GetFullPath(Path.IsPathRooted(root) ? root : Path.Combine(env.ContentRootPath, root));
        var articlesRoot = config["Images:ArticlesRoot"] ?? "Articles";
        _articlesRoot = Path.GetFullPath(Path.IsPathRooted(articlesRoot) ? articlesRoot : Path.Combine(env.ContentRootPath, articlesRoot));
        _remoteBase = config["Images:RemoteBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(_remoteBase)) _remoteBase = null;
    }

    public string Placeholder => "/images/default.webp";

    public string ArticlesRoot => _articlesRoot;

    public string Resolve(string? dbPath)
    {
        var path = Normalize(dbPath);
        if (path is null) return Placeholder;
        if (MapToLocalFile(path) is not null) return Encode(path);
        return _remoteBase is not null ? _remoteBase + Encode(path) : Placeholder;
    }

    public string? MapToLocalFile(string? dbPath)
    {
        var path = Normalize(dbPath);
        if (path is null) return null;
        return _fileCache.GetOrAdd(path, p =>
        {
            var physical = ToPhysical(p);
            return physical is not null && File.Exists(physical) ? physical : null;
        });
    }

    public string? MapToLocalDirectory(string? dbPath)
    {
        var path = Normalize(dbPath);
        if (path is null) return null;
        var slash = path.LastIndexOf('/');
        if (slash <= 0) return null;
        var physical = ToPhysical(path[..(slash + 1)] + "x");
        var dir = physical is null ? null : Path.GetDirectoryName(physical);
        return dir is not null && Directory.Exists(dir) ? dir : null;
    }

    public string? FindFolder(string virtualPrefix, string folderSlug)
    {
        var entry = Map.FirstOrDefault(m => m.Prefix.Equals(virtualPrefix, StringComparison.OrdinalIgnoreCase));
        if (entry.Folder is null) return null;
        var start = Path.Combine(_root, entry.Folder);
        if (!Directory.Exists(start)) return null;
        return Directory.EnumerateDirectories(start, "*", SearchOption.AllDirectories)
            .FirstOrDefault(d => Slugger.Slugify(Path.GetFileName(d)) == folderSlug);
    }

    public string? FindArticleFile(string imageFolder)
    {
        var folder = Path.Combine(_articlesRoot, Path.GetRelativePath(_root, imageFolder));
        return Directory.Exists(folder) ? Directory.EnumerateFiles(folder, "*.docx").FirstOrDefault() : null;
    }

    public string ToVirtualPath(string physicalFile)
    {
        var full = Path.GetFullPath(physicalFile);
        foreach (var (prefix, folder) in Map)
        {
            if (!prefix.EndsWith('/')) continue;
            var baseDir = Path.Combine(_root, folder) + Path.DirectorySeparatorChar;
            if (full.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                return prefix + full[baseDir.Length..].Replace(Path.DirectorySeparatorChar, '/');
        }
        return Placeholder;
    }

    private string? ToPhysical(string path)
    {
        foreach (var (prefix, folder) in Map)
        {
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var rest = path[prefix.Length..].Replace('/', Path.DirectorySeparatorChar);
            var physical = Path.GetFullPath(Path.Combine(_root, folder, rest));
            // Never leave the image root, whatever the path contains.
            return physical.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? physical : null;
        }
        return null;
    }

    private static string? Normalize(string? dbPath)
    {
        if (string.IsNullOrWhiteSpace(dbPath)) return null;
        var p = dbPath.Trim().Replace('\\', '/');
        if (p.StartsWith("~/")) p = p[1..];
        if (!p.StartsWith('/')) p = "/" + p;
        return p.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ? p : null;
    }

    private static string Encode(string path) =>
        string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    public static string SniffContentType(string physicalFile)
    {
        // SVG is text, so it has no magic bytes to sniff.
        if (physicalFile.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) return "image/svg+xml";
        Span<byte> header = stackalloc byte[12];
        using (var fs = File.OpenRead(physicalFile))
            if (fs.Read(header) < 12) return "application/octet-stream";
        if (header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        if (header[4..8].SequenceEqual("ftyp"u8)) return "image/avif";
        if (header[0] == 0x89 && header[1..4].SequenceEqual("PNG"u8)) return "image/png";
        if (header[0] == 0xFF && header[1] == 0xD8) return "image/jpeg";
        return "application/octet-stream";
    }
}
