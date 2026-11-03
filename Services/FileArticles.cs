using System.Security.Cryptography;
using System.Text.Json;
using BikeCarHub.Domain;

namespace BikeCarHub.Services;

/// <summary>One article published from a Word file instead of a database row (see Articles/articles.json).</summary>
public sealed class FileArticleEntry
{
    /// <summary>"blog", "news" or "value-for-money".</summary>
    public string? Section { get; set; }
    /// <summary>Path of the .docx under the articles root, for example "blogs/2026/Nov/my-article.docx".</summary>
    public string? Docx { get; set; }
    /// <summary>Web folder with the article's images, for example "/images/blogs/2026/Nov/my-article".</summary>
    public string? ImageFolder { get; set; }
    /// <summary>URL slug; defaults to the image folder name.</summary>
    public string? Slug { get; set; }
    /// <summary>Publish date (yyyy-MM-dd); defaults to the Word file's created date.</summary>
    public DateTime? Date { get; set; }
    /// <summary>File name, inside the image folder, of the smaller picture used on article cards.</summary>
    public string? CardImage { get; set; }
    public bool TwoWheeler { get; set; } = true;
    /// <summary>false leaves out the "More photos" section (folder pictures that are not in the Word file).</summary>
    public bool MorePhotos { get; set; } = true;
    /// <summary>true keeps the entry off the site.</summary>
    public bool Draft { get; set; }
}

public static class FileArticles
{
    public const string ManifestName = "articles.json";
    private static readonly string[] ImageExtensions = [".webp", ".avif", ".png", ".jpg", ".jpeg"];

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyList<FileArticleEntry> ReadManifest(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<List<FileArticleEntry>>(File.ReadAllText(path), Json) ?? [] : [];

    public static ArticleKind? KindOf(string? section) => section?.Trim().ToLowerInvariant() switch
    {
        "blog" or "blogs" => ArticleKind.Blog,
        "news" => ArticleKind.News,
        "value-for-money" => ArticleKind.ValueForMoney,
        _ => null
    };

    /// <summary>Builds the article, or returns null with the reason when the entry cannot be published.</summary>
    public static (Article? Article, string? Problem) Build(FileArticleEntry entry, ArticleKind kind, int id, IImageUrlProvider images)
    {
        if (string.IsNullOrWhiteSpace(entry.Docx)) return (null, "no docx path");
        var root = Path.GetFullPath(images.ArticlesRoot);
        var docxPath = Path.GetFullPath(Path.Combine(root, entry.Docx.Replace('/', Path.DirectorySeparatorChar)));
        if (!docxPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return (null, "docx path leaves the articles folder");
        if (!File.Exists(docxPath)) return (null, $"file not found: {entry.Docx}");

        var virtualFolder = entry.ImageFolder?.Trim().TrimEnd('/');
        var folder = virtualFolder is null ? null : images.MapToLocalDirectory(virtualFolder + "/x");

        // Pictures in the Word file are matched to the published web files by content, so file names do not matter.
        var byHash = new Dictionary<string, string>();
        var folderImages = folder is null ? [] : Directory.EnumerateFiles(folder)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var file in folderImages) byHash.TryAdd(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))), file);

        DocxImage? Resolve(byte[] bytes)
        {
            if (!byHash.TryGetValue(Convert.ToHexString(SHA256.HashData(bytes)), out var file)) return null;
            var size = ImageSize.Read(file) ?? ImageSize.Read(bytes) ?? (1200, 675);
            var path = images.ToVirtualPath(file);
            return new DocxImage(images.Resolve(path), path, size.Width, size.Height);
        }

        var doc = RichDocxReader.Read(docxPath, Resolve, images.Placeholder);
        if (doc?.Title is null) return (null, "the Word file has no title or text");

        var slug = Slugger.Slugify(entry.Slug ?? (folder is null ? doc.Title : Path.GetFileName(folder)));
        string? card = null;
        if (entry.CardImage is not null && folder is not null && File.Exists(Path.Combine(folder, entry.CardImage)))
            card = images.ToVirtualPath(Path.Combine(folder, entry.CardImage));

        var article = new Article
        {
            Id = id,
            Kind = kind,
            Title = doc.Title,
            Slug = slug,
            Summary = doc.Standfirst,
            ImagePath = doc.Cover?.Image.VirtualPath ?? card,
            Date = entry.Date ?? doc.Created?.Date,
            IsTwoWheeler = entry.TwoWheeler,
            SeoTitle = doc.DocumentTitle is { } t && Slugger.Slugify(t) != Slugger.Slugify(doc.Title) ? t : null,
            Author = doc.Author,
            WordLayout = true,
            WordTheme = doc.Theme,
            Kicker = doc.Kicker,
            CoverAlt = doc.Cover?.Alt,
            CoverCaption = doc.Cover?.Caption,
            CoverWidth = doc.Cover?.Image.Width,
            CoverHeight = doc.Cover?.Image.Height,
            CardImagePath = card,
            Faqs = doc.Faqs,
            BodyHtml = doc.BodyHtml,
            BodyTextLength = doc.TextLength + (doc.Standfirst?.Length ?? 0)
        };

        // Photos supplied in the folder but not placed in the text go in the "More photos" gallery.
        var shown = new HashSet<string>(doc.UsedImages, StringComparer.OrdinalIgnoreCase);
        if (card is not null) shown.Add(card);
        if (entry.MorePhotos) article.GalleryPaths.AddRange(folderImages.Select(images.ToVirtualPath).Where(p => !shown.Contains(p)));

        return (article, doc.MissingImages > 0 ? $"{doc.MissingImages} picture(s) in the Word file have no matching file in {entry.ImageFolder}" : null);
    }
}
