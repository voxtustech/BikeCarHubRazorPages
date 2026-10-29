using System.Text.Json;
using BikeCarHub.Domain;
using BikeCarHub.Services;

namespace BikeCarHub.Seo;

public sealed class SiteSettings(IConfiguration config)
{
    public string Name { get; } = config["Site:Name"] ?? "BikeCarHub";
    public string BaseUrl { get; } = (config["Site:BaseUrl"] ?? "").TrimEnd('/');
    public string? Phone { get; } = config["Site:Phone"];
    public string? Email { get; } = config["Site:Email"];

    public string Absolute(string pathOrUrl) =>
        pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? pathOrUrl : BaseUrl + pathOrUrl;
}

public sealed record Crumb(string Name, string? Path);

/// <summary>Everything the layout needs for the head of one page.</summary>
public sealed class PageMeta
{
    public string Title { get; set; } = "BikeCarHub";
    public string Description { get; set; } = "";
    /// <summary>Path (with any query) of the canonical URL. Defaults to the request path.</summary>
    public string? CanonicalPath { get; set; }
    public bool NoIndex { get; set; }
    public string OgType { get; set; } = "website";
    /// <summary>Resolved image URL for social cards.</summary>
    public string? Image { get; set; }
    /// <summary>Resolved URL of the largest above-the-fold image, preloaded by the layout.</summary>
    public string? PreloadImage { get; set; }
    public List<Crumb> Breadcrumbs { get; } = new();
    public List<object> JsonLd { get; } = new();

    public string Robots => NoIndex ? "noindex,follow" : "index,follow";

    public PageMeta Crumbs(params (string Name, string? Path)[] crumbs)
    {
        Breadcrumbs.Add(new Crumb("Home", "/"));
        Breadcrumbs.AddRange(crumbs.Select(c => new Crumb(c.Name, c.Path)));
        return this;
    }
}

/// <summary>Structured data built only from values that exist in the database.</summary>
public static class JsonLd
{
    public static string Serialize(object data) => JsonSerializer.Serialize(data);

    public static object Organization(SiteSettings site, string logoUrl)
    {
        var org = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["name"] = site.Name,
            ["url"] = site.BaseUrl + "/",
            ["logo"] = site.Absolute(logoUrl)
        };
        if (!string.IsNullOrWhiteSpace(site.Email)) org["email"] = site.Email;
        if (!string.IsNullOrWhiteSpace(site.Phone)) org["telephone"] = site.Phone;
        return org;
    }

    public static object Breadcrumbs(SiteSettings site, IReadOnlyList<Crumb> crumbs, string currentPath) => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = crumbs.Select((c, i) => new Dictionary<string, object?>
        {
            ["@type"] = "ListItem",
            ["position"] = i + 1,
            ["name"] = c.Name,
            ["item"] = site.Absolute(c.Path ?? currentPath)
        }).ToList()
    };

    public static object ForProduct(SiteSettings site, Bike bike, Variant? variant, string imageUrl, bool includeOffer)
    {
        var product = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = variant?.Name ?? bike.Name,
            ["category"] = bike.IsScooter ? "Scooter" : "Motorcycle",
            ["brand"] = new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = bike.Brand.Name },
            ["image"] = site.Absolute(imageUrl),
            ["url"] = site.Absolute(variant?.Url ?? bike.Url)
        };
        if (bike.Description is not null) product["description"] = Formatting.Truncate(bike.Description, 300);
        // An Offer is emitted only when a real price exists.
        if (includeOffer && bike.MinPrice is { } low)
        {
            product["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "AggregateOffer",
                ["priceCurrency"] = "INR",
                ["lowPrice"] = low,
                ["highPrice"] = bike.MaxPrice ?? low,
                ["url"] = site.Absolute(bike.Url)
            };
        }
        return product;
    }

    public static object ForArticle(SiteSettings site, Article article, string imageUrl)
    {
        var data = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = article.Kind == ArticleKind.News ? "NewsArticle" : "Article",
            ["headline"] = Formatting.Truncate(article.Title, 110),
            ["mainEntityOfPage"] = site.Absolute(article.Url),
            ["image"] = site.Absolute(imageUrl),
            ["publisher"] = new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = site.Name }
        };
        if (article.Date is { } date) data["datePublished"] = date.ToString("yyyy-MM-dd");
        return data;
    }
}
