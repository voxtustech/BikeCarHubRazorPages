using System.Text.RegularExpressions;
using BikeCarHub.Domain;
using BikeCarHub.Services;

namespace BikeCarHub.Seo;

/// <summary>
/// Answers every address the previous site used with one permanent redirect to the page that replaced it,
/// 410 Gone where nothing replaced it, or lets the request fall through to a real 404.
/// Runs before the lowercase redirect so an old mixed-case link takes a single hop, not two.
/// </summary>
public static class LegacyUrls
{
    private static readonly Dictionary<string, string> Fixed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/contact-us"] = "/ask-a-question",
        ["/ask-question"] = "/ask-a-question",
        ["/privacy-policy"] = "/privacy",
        ["/terms-and-conditions"] = "/terms",
        ["/login"] = "/account/sign-in",
        ["/account/login"] = "/account/sign-in",
        ["/register"] = "/account/sign-up",
        ["/brands"] = "/new-bikes",
        ["/home"] = "/",
        ["/home/index"] = "/",
        ["/blogs"] = "/blog",
        ["/latestnews"] = "/news",
        ["/latestnews/index"] = "/news",
        ["/latest-news"] = "/news",
        ["/valueformoney"] = "/value-for-money",
        ["/valueformoney/index"] = "/value-for-money",
        ["/upcoming-bike"] = "/upcoming-bikes",
        ["/upcomingbike"] = "/upcoming-bikes",
        ["/upcomingbike/twupcomingbikedetails"] = "/upcoming-bikes",
        ["/compare/twcompare"] = "/compare",
        ["/home/twowheelerdetails"] = "/new-bikes",
        ["/home/twbranddetails"] = "/new-bikes"
    };

    // Old JSON endpoints, the old admin login and features the new site does not have.
    private static readonly Regex Removed = new(
        @"^/(adminlogin(/.*)?|my-reviews|home/get\w+|compare/get\w+|wishlist/get\w+|[^/]+/gettwspecdata|[a-z0-9-]+-features-and-specifications)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly (string Old, ArticleKind Kind)[] ArticleSections =
    [
        ("blogs", ArticleKind.Blog), ("blog", ArticleKind.Blog),
        ("latestnews", ArticleKind.News), ("latest-news", ArticleKind.News), ("news", ArticleKind.News),
        ("value-for-money", ArticleKind.ValueForMoney), ("valueformoney", ArticleKind.ValueForMoney)
    ];

    // First segments that belong to the new site, so they are never read as an old brand folder.
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "new-bikes", "upcoming-bikes", "electric-bikes", "compare", "account", "wishlist", "status", "search",
        "images", "css", "js", "lib", "sitemaps", "health", "about", "privacy", "terms", "disclaimer",
        "emi-calculator", "ask-a-question", "newsletter", "news", "blog", "value-for-money",
        "robots.txt", "sitemap.xml", "favicon.ico"
    };

    public static IApplicationBuilder UseLegacyUrls(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            await next();
            return;
        }

        string? target;
        try
        {
            target = await ResolveAsync(context);
        }
        catch (Exception)
        {
            // Without the database the old addresses cannot be resolved; the page itself reports the outage.
            target = null;
        }
        switch (target)
        {
            case null:
                await next();
                return;
            case "":
                context.Response.StatusCode = StatusCodes.Status410Gone;
                // Let UseStatusCodePagesWithReExecute render the friendly page.
                return;
            default:
                context.Response.Redirect(target, permanent: true);
                return;
        }
    });

    /// <summary>The new address, "" for 410 Gone, or null to let the request continue.</summary>
    private static async Task<string?> ResolveAsync(HttpContext context)
    {
        var path = Uri.UnescapeDataString(context.Request.Path.Value ?? "/").TrimEnd('/');
        if (path.Length == 0) return null;

        if (Fixed.TryGetValue(path, out var fixedTarget))
        {
            // The old detail pages carried the record id in the query string.
            if (path.Equals("/home/twowheelerdetails", StringComparison.OrdinalIgnoreCase) && TryId(context, "modelId", out var modelId))
                return await ModelByIdAsync(context, modelId) ?? fixedTarget;
            if (path.Equals("/home/twbranddetails", StringComparison.OrdinalIgnoreCase) && TryId(context, "brandId", out var brandId))
                return await BrandByIdAsync(context, brandId) ?? fixedTarget;
            return fixedTarget;
        }

        if (Removed.IsMatch(path)) return "";

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 2)
        {
            var section = ArticleSections.FirstOrDefault(s => s.Old.Equals(segments[0], StringComparison.OrdinalIgnoreCase));
            if (section.Old is not null) return await ArticleAsync(context, section, segments[1]);

            if (segments[0].Equals("upcoming-bike", StringComparison.OrdinalIgnoreCase)
                || (segments[0].Equals("upcoming-bikes", StringComparison.OrdinalIgnoreCase) && HasOldSpelling(segments[1])))
                return await UpcomingAsync(context, segments[1]);
        }

        if (segments.Length is 1 or 2 && !Reserved.Contains(segments[0]))
            return await BrandFolderAsync(context, segments);

        return null;
    }

    private static async Task<string?> ArticleAsync(HttpContext context, (string Old, ArticleKind Kind) section, string slug)
    {
        var all = (await Content(context).GetAsync()).Of(section.Kind);
        var article = Slugger.Match(all, slug, a => a.Slug);
        // A current address that already matches is served by the article page itself.
        if (article is not null) return context.Request.Path.Value == article.Url ? null : article.Url;
        // Unknown article on a current address: the article page answers 404. On an old address it is gone.
        var isCurrent = section.Old is "blog" or "news" or "value-for-money";
        return isCurrent ? null : "";
    }

    private static async Task<string?> UpcomingAsync(HttpContext context, string slug)
    {
        var content = await Content(context).GetAsync();
        var bike = Slugger.Match(content.Upcoming, slug, u => u.Slug);
        if (bike is not null)
        {
            // A bike that has launched since is better served by its model page than by a thin preview.
            var target = bike.ReleasedAs is not null && bike.BodyTextLength < 200 ? bike.ReleasedAs.Url : bike.Url;
            return context.Request.Path.Value == target ? null : target;
        }
        var catalog = await Catalog(context).GetAsync();
        var launched = Slugger.Match(catalog.Bikes, slug, b => b.FullSlug);
        return launched?.Url ?? "";
    }

    /// <summary>The old site served brands at /{brand} and models at /{brand}/{model}, with spaces in the brand ("royal enfield").</summary>
    private static async Task<string?> BrandFolderAsync(HttpContext context, string[] segments)
    {
        var catalog = await Catalog(context).GetAsync();
        if (!catalog.BrandBySlug.TryGetValue(Slugger.Slugify(segments[0]), out var brand)) return null;
        if (segments.Length == 1) return brand.Url;

        var wanted = Slugger.Slugify(segments[1]);
        // Old model slugs were the brand slug plus the model ("royal-enfield-classic-350") or the full model name
        // ("ola-roadster" for Roadster under Ola Electric).
        var bike = Slugger.Match(brand.Bikes, wanted, b => b.Slug)
            ?? Slugger.Match(brand.Bikes, wanted, b => b.FullSlug)
            ?? Slugger.Match(brand.Bikes, wanted, b => Slugger.Slugify(b.Name));
        if (bike is not null) return bike.Url;

        var removedSlug = wanted.StartsWith(brand.Slug + "-", StringComparison.Ordinal) ? wanted[(brand.Slug.Length + 1)..] : wanted;
        if (catalog.RemovedKeys.Contains($"{brand.Slug}/{removedSlug}") || catalog.RemovedKeys.Contains($"{brand.Slug}/{wanted}")) return "";
        // A model this site never had: a real 404, not a redirect to an unrelated page.
        return null;
    }

    private static async Task<string?> ModelByIdAsync(HttpContext context, int id)
    {
        var catalog = await Catalog(context).GetAsync();
        return catalog.Bikes.FirstOrDefault(b => b.Id == id)?.Url;
    }

    private static async Task<string?> BrandByIdAsync(HttpContext context, int id)
    {
        var catalog = await Catalog(context).GetAsync();
        return catalog.Brands.FirstOrDefault(b => b.Id == id)?.Url;
    }

    private static bool HasOldSpelling(string slug) => Slugger.Slugify(slug) != slug;

    private static bool TryId(HttpContext context, string key, out int id) =>
        int.TryParse(context.Request.Query[key].ToString(), out id) && id > 0;

    private static IContentService Content(HttpContext context) => context.RequestServices.GetRequiredService<IContentService>();
    private static ICatalogService Catalog(HttpContext context) => context.RequestServices.GetRequiredService<ICatalogService>();
}
