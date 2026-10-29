using BikeCarHub.Data;
using BikeCarHub.Domain;

namespace BikeCarHub.Services;

public sealed class SiteContent
{
    public required IReadOnlyList<Article> Blogs { get; init; }
    public required IReadOnlyList<Article> News { get; init; }
    public required IReadOnlyList<Article> ValueForMoney { get; init; }
    public required IReadOnlyList<UpcomingBike> Upcoming { get; init; }
    public required IReadOnlyList<HeroSlide> HeroSlides { get; init; }
    public required IReadOnlyList<(Bike A, Bike B, string Topic)> ComparePairs { get; init; }

    public IReadOnlyList<Article> Of(ArticleKind kind) => kind switch
    {
        ArticleKind.Blog => Blogs,
        ArticleKind.News => News,
        _ => ValueForMoney
    };
}

public interface IContentService
{
    Task<SiteContent> GetAsync();
}

public sealed class ContentService(IContentRepository repo, ICatalogService catalogService, IImageUrlProvider images, ICacheStore cache) : IContentService
{
    private static readonly string[] ImageExtensions = [".webp", ".avif", ".png", ".jpg", ".jpeg"];

    public Task<SiteContent> GetAsync() => cache.GetOrCreateAsync("content", TimeSpan.FromMinutes(10), LoadAsync);

    private async Task<SiteContent> LoadAsync()
    {
        var catalog = await catalogService.GetAsync();

        var blogSlugs = new HashSet<string>();
        var blogs = (await repo.GetBlogsAsync())
            .Where(r => !string.IsNullOrWhiteSpace(r.BlogHeading))
            .OrderBy(r => r.BlogId)
            .Select(r => BuildArticle(ArticleKind.Blog, r.BlogId, r.BlogHeading!, r.URL, r.BlogSummary, r.BlogDetail, r.ImageURL, r.Date, r.IsTwoWheeler, blogSlugs))
            .ToList();

        var newsSlugs = new HashSet<string>();
        var news = (await repo.GetNewsAsync())
            .Where(r => !string.IsNullOrWhiteSpace(r.NewsHeading))
            .OrderBy(r => r.TWLatestNewsId)
            .Select(r => BuildArticle(ArticleKind.News, r.TWLatestNewsId, r.NewsHeading!, r.ImageURL, r.NewsSummary, r.NewsDetail, r.ImageFolderURL, r.Date, r.IsTwoWheeler, newsSlugs))
            .ToList();

        var vfmSlugs = new HashSet<string>();
        var vfm = (await repo.GetValueForMoneyAsync())
            .Where(r => !string.IsNullOrWhiteSpace(r.VFMHeading))
            .OrderBy(r => r.VFMId)
            .Select(r => BuildArticle(ArticleKind.ValueForMoney, r.VFMId, r.VFMHeading!, r.URL, null, null, r.ImageURL, r.Date, r.IsTwoWheeler, vfmSlugs))
            .ToList();

        var upcomingSlugs = new HashSet<string>();
        var upcoming = (await repo.GetUpcomingAsync())
            .Where(r => !string.IsNullOrWhiteSpace(r.UpcomingBikeName))
            .OrderBy(r => r.UpcomingBikeId)
            .Select(r => BuildUpcoming(r, catalog, upcomingSlugs))
            .ToList();

        var slides = (await repo.GetHeroSlidesAsync())
            .Where(r => !string.IsNullOrWhiteSpace(r.Title))
            .Select(r => new HeroSlide(r.Title!.Trim(), r.Subtitle, r.ImageURL, r.Badge, r.Featured, RepairRupee(r.Price)))
            .ToList();

        var pairs = new List<(Bike, Bike, string)>();
        foreach (var topic in await repo.GetCompareTopicsAsync())
        {
            var parts = topic.Split(" vs ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && catalog.FindBikeByName(parts[0]) is { } a && catalog.FindBikeByName(parts[1]) is { } b && a != b)
                pairs.Add((a, b, topic.Trim()));
        }

        return new SiteContent
        {
            Blogs = blogs.OrderByDescending(a => a.Date).ToList(),
            News = news.OrderByDescending(a => a.Date).ToList(),
            ValueForMoney = vfm.OrderByDescending(a => a.Date).ToList(),
            Upcoming = upcoming.OrderByDescending(u => u.ExpectedDate).ToList(),
            HeroSlides = slides,
            ComparePairs = pairs
        };
    }

    private Article BuildArticle(ArticleKind kind, int id, string title, string? urlColumn, string? summary, string? body,
        string? imagePath, DateTime? date, bool? isTwoWheeler, HashSet<string> used)
    {
        // The old site stored "blogs/some-slug"; keep the last segment so existing links can be redirected.
        var lastSegment = urlColumn?.Trim().TrimEnd('/').Split('/').LastOrDefault();
        var slug = Slugger.Slugify(string.IsNullOrWhiteSpace(lastSegment) ? title : lastSegment);
        var article = new Article
        {
            Id = id,
            Kind = kind,
            Title = title.Trim(),
            Slug = Slugger.Unique(slug, id, used),
            Summary = CleanText(summary),
            ImagePath = imagePath,
            Date = date,
            IsTwoWheeler = isTwoWheeler == true
        };

        (article.BodyHtml, article.BodyTextLength) = Formatting.TextToHtml(body);

        // Longer articles live as a Word file next to their images; prefer it when it has more text.
        if (images.MapToLocalDirectory(imagePath) is { } folder)
        {
            var docx = Directory.EnumerateFiles(folder, "*.docx").FirstOrDefault();
            if (docx is not null)
            {
                var (html, length) = DocxArticleReader.Read(docx, article.Title);
                if (html is not null && length > article.BodyTextLength)
                {
                    (article.BodyHtml, article.BodyTextLength) = (html, length);
                    var header = Path.GetFileName(imagePath ?? "");
                    article.GalleryPaths.AddRange(ImagesIn(folder)
                        .Where(f => !Path.GetFileName(f).Equals(header, StringComparison.OrdinalIgnoreCase))
                        .Select(images.ToVirtualPath));
                }
            }
        }
        return article;
    }

    private UpcomingBike BuildUpcoming(UpcomingRow r, Catalog catalog, HashSet<string> used)
    {
        var name = r.UpcomingBikeName!.Trim();
        var lastSegment = r.ImageURL?.Trim().TrimEnd('/').Split('/').LastOrDefault();
        var slug = Slugger.Slugify(string.IsNullOrWhiteSpace(lastSegment) ? name : lastSegment);
        var bike = new UpcomingBike
        {
            Id = r.UpcomingBikeId,
            Name = name,
            Slug = Slugger.Unique(slug, r.UpcomingBikeId, used),
            BrandName = string.IsNullOrWhiteSpace(r.BrandName) ? null : r.BrandName.Trim(),
            ExpectedText = string.IsNullOrWhiteSpace(r.ExpectedLaunchDate) ? null : r.ExpectedLaunchDate.Trim(),
            ExpectedDate = r.FilterLaunchDate,
            Created = r.CreatedDateTime,
            ReleasedAs = catalog.FindBikeByName(name)
        };

        var details = r.UpcomingBikeDetails?.Trim();
        if (details is { Length: >= 20 }) (bike.BodyHtml, bike.BodyTextLength) = Formatting.TextToHtml(details);

        // The table has no image column; use the folder named after the bike when there is one.
        if (images.FindFolder("/images/upcoming/", Slugger.Slugify(name)) is { } folder)
        {
            var files = ImagesIn(folder).ToList();
            var cover = files.FirstOrDefault(f => Path.GetFileName(f).Contains("header", StringComparison.OrdinalIgnoreCase)) ?? files.FirstOrDefault();
            if (cover is not null) bike.ImagePath = images.ToVirtualPath(cover);
            var docx = Directory.EnumerateFiles(folder, "*.docx").FirstOrDefault();
            if (docx is not null)
            {
                var (html, length) = DocxArticleReader.Read(docx, null);
                if (html is not null && length > bike.BodyTextLength) (bike.BodyHtml, bike.BodyTextLength) = (html, length);
            }
        }
        return bike;
    }

    // Some rows hold the literal text "NULL" instead of a database null.
    private static string? CleanText(string? value)
    {
        var v = value?.Trim();
        return string.IsNullOrEmpty(v) || v.Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : v;
    }

    private static IEnumerable<string> ImagesIn(string folder) =>
        Directory.EnumerateFiles(folder)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => int.TryParse(Path.GetFileNameWithoutExtension(f), out var n) ? n : int.MaxValue)
            .ThenBy(f => f, StringComparer.OrdinalIgnoreCase);

    // The rupee sign was stored in a varchar column and came back as "?".
    private static string? RepairRupee(string? price)
    {
        var p = price?.Trim();
        if (string.IsNullOrEmpty(p)) return null;
        return p.Length > 1 && p[0] == '?' && char.IsDigit(p[1]) ? "₹" + p[1..] : p;
    }
}
