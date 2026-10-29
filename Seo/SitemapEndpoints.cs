using System.Text;
using System.Xml.Linq;
using BikeCarHub.Domain;
using BikeCarHub.Services;

namespace BikeCarHub.Seo;

public static class SitemapEndpoints
{
    private const int ChunkSize = 10_000;
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private sealed record Entry(string Path, DateTime? LastModified);

    public static void MapSeoEndpoints(this WebApplication app)
    {
        app.MapGet("/robots.txt", (SiteSettings site) => Results.Text(
            $"""
            User-agent: *
            Disallow: /search
            Disallow: /account/
            Disallow: /newsletter
            Disallow: /compare/go
            Disallow: /status/

            Sitemap: {site.BaseUrl}/sitemap.xml

            """, "text/plain", Encoding.UTF8));

        app.MapGet("/sitemap.xml", async (SiteSettings site, ICatalogService catalog, IContentService content, ICacheStore cache) =>
        {
            var xml = await cache.GetOrCreateAsync("sitemap:index", TimeSpan.FromMinutes(30), async () =>
            {
                var groups = await BuildAsync(catalog, content);
                var index = new XElement(Ns + "sitemapindex");
                foreach (var (name, entries) in groups)
                {
                    if (entries.Count == 0) continue;
                    var chunks = (entries.Count + ChunkSize - 1) / ChunkSize;
                    for (var i = 1; i <= chunks; i++)
                    {
                        var node = new XElement(Ns + "sitemap", new XElement(Ns + "loc", $"{site.BaseUrl}/sitemaps/{name}-{i}.xml"));
                        var latest = entries.Skip((i - 1) * ChunkSize).Take(ChunkSize).Max(e => e.LastModified);
                        if (latest is { } d) node.Add(new XElement(Ns + "lastmod", d.ToString("yyyy-MM-dd")));
                        index.Add(node);
                    }
                }
                return Render(index);
            });
            return Xml(xml);
        });

        app.MapGet("/sitemaps/{file}.xml", async (string file, SiteSettings site, ICatalogService catalog, IContentService content, ICacheStore cache) =>
        {
            var dash = file.LastIndexOf('-');
            if (dash <= 0 || !int.TryParse(file[(dash + 1)..], out var chunk) || chunk < 1) return Results.NotFound();
            var name = file[..dash];
            var groups = await BuildAsync(catalog, content);
            if (!groups.TryGetValue(name, out var entries) || (chunk - 1) * ChunkSize >= entries.Count) return Results.NotFound();

            var xml = await cache.GetOrCreateAsync($"sitemap:{name}:{chunk}", TimeSpan.FromMinutes(30), () =>
            {
                var set = new XElement(Ns + "urlset");
                foreach (var e in entries.Skip((chunk - 1) * ChunkSize).Take(ChunkSize))
                {
                    var node = new XElement(Ns + "url", new XElement(Ns + "loc", site.BaseUrl + e.Path));
                    if (e.LastModified is { } d) node.Add(new XElement(Ns + "lastmod", d.ToString("yyyy-MM-dd")));
                    set.Add(node);
                }
                return Task.FromResult(Render(set));
            });
            return Xml(xml);
        });
    }

    private static async Task<Dictionary<string, List<Entry>>> BuildAsync(ICatalogService catalogService, IContentService contentService)
    {
        var catalog = await catalogService.GetAsync();
        var content = await contentService.GetAsync();

        var brands = new List<Entry>();
        foreach (var b in catalog.Brands)
        {
            brands.Add(new Entry(b.Url, b.Created));
            // Facet landing pages are listed only where they have results.
            if (b.Bikes.Any(x => x.IsScooter) && b.Bikes.Any(x => !x.IsScooter))
            {
                brands.Add(new Entry($"{b.Url}/bikes", b.Created));
                brands.Add(new Entry($"{b.Url}/scooters", b.Created));
            }
            if (b.Bikes.Any(x => x.IsEv) && b.Bikes.Any(x => !x.IsEv))
            {
                brands.Add(new Entry($"{b.Url}/electric", b.Created));
                brands.Add(new Entry($"{b.Url}/petrol", b.Created));
            }
        }

        static List<Entry> Articles(IEnumerable<Article> list) =>
            list.Where(a => a.IsIndexable).Select(a => new Entry(a.Url, a.Date)).ToList();

        return new Dictionary<string, List<Entry>>
        {
            ["pages"] =
            [
                new("/", null), new("/new-bikes", null), new("/upcoming-bikes", null), new("/news", null), new("/blog", null),
                new("/value-for-money", null), new("/compare", null), new("/emi-calculator", null), new("/ask-a-question", null),
                new("/about", null), new("/privacy", null), new("/terms", null), new("/disclaimer", null)
            ],
            ["brands"] = brands,
            ["models"] = catalog.Bikes.Select(b => new Entry(b.Url, b.Created)).ToList(),
            ["variants"] = catalog.Bikes.SelectMany(b => b.Variants).Select(v => new Entry(v.Url, v.Created)).ToList(),
            ["upcoming"] = content.Upcoming.Where(u => u.BodyTextLength >= 200).Select(u => new Entry(u.Url, u.Created)).ToList(),
            ["news"] = Articles(content.News),
            ["blogs"] = Articles(content.Blogs),
            ["value-for-money"] = Articles(content.ValueForMoney)
        };
    }

    private static string Render(XElement root) =>
        new XDeclaration("1.0", "utf-8", null) + Environment.NewLine + root.ToString(SaveOptions.DisableFormatting);

    private static IResult Xml(string xml) => Results.Text(xml, "application/xml", Encoding.UTF8);
}
