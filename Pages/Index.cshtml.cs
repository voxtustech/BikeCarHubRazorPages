using BikeCarHub.Domain;
using BikeCarHub.Seo;
using BikeCarHub.Services;
using BikeCarHub.Web;

namespace BikeCarHub.Pages;

public class IndexModel(ICatalogService catalogService, IContentService contentService, IImageUrlProvider images, SiteSettings site) : BasePageModel
{
    public sealed record Tile(string Label, string Note, string Href, int Count);
    public sealed record ComparePick(Bike A, Bike B, string Href, string ImagePath);

    public Catalog Catalog { get; private set; } = null!;
    public SiteContent Editorial { get; private set; } = null!;
    public int VariantCount { get; private set; }
    public IReadOnlyList<Tile> BudgetTiles { get; private set; } = [];
    public IReadOnlyList<Tile> StyleTiles { get; private set; } = [];
    public IReadOnlyList<Brand> TopBrands { get; private set; } = [];
    public IReadOnlyList<Brand> OtherBrands { get; private set; } = [];
    public IReadOnlyList<Bike> LatestLaunches { get; private set; } = [];
    public IReadOnlyList<ComparePick> ComparePicks { get; private set; } = [];
    public Article? LeadStory { get; private set; }
    public IReadOnlyList<Article> MoreStories { get; private set; } = [];
    public IReadOnlyList<Article> News { get; private set; } = [];
    public IReadOnlyList<Article> SmartBuys { get; private set; } = [];
    public IReadOnlyList<UpcomingBike> Radar { get; private set; } = [];
    public EmiResult EmiExample { get; } = EmiCalculator.Calculate(100_000, 10, 3);

    public static readonly (string Value, string Label)[] Budgets =
    [
        ("0-100000", "Under ₹1 lakh"), ("100000-200000", "₹1 – 2 lakh"),
        ("200000-300000", "₹2 – 3 lakh"), ("300000-0", "Above ₹3 lakh")
    ];

    // The previous site used short file names for a few comparison images.
    private static readonly Dictionary<string, string> CompareImageAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Royal Enfield Bullet 350 vs TVS Apache RR 310"] = "royalvsapache",
        ["Bajaj Pulsar 150 vs Bajaj Pulsar 125"] = "pulsarvspulsar",
        ["TVS Apache RTR 160 4V vs Bajaj Dominar 250"] = "apachevsdominar",
        ["Yamaha R3 vs Bajaj Pulsar NS200"] = "yamahavspulsar"
    };

    /// <summary>Image for a comparison card: the topic's own file, the same pair reversed, a known short name, then the default.</summary>
    private string CompareImage(string topic)
    {
        var parts = topic.Split(" vs ", StringSplitOptions.TrimEntries);
        var names = new List<string> { topic };
        if (parts.Length == 2) names.Add($"{parts[1]} vs {parts[0]}");
        if (CompareImageAliases.TryGetValue(topic, out var alias)) names.Add(alias);
        foreach (var name in names)
        {
            var path = $"/images/CompareBikes/{name}.webp";
            if (images.MapToLocalFile(path) is not null) return path;
        }
        return "/images/CompareBikes/default.webp";
    }

    public async Task OnGetAsync()
    {
        Catalog = await catalogService.GetAsync();
        Editorial = await contentService.GetAsync();
        var bikes = Catalog.Bikes;
        VariantCount = bikes.Sum(b => b.Variants.Count);

        // Every count on the page is worked out from the catalogue, never typed in.
        int Count(BikeQuery query) => query.Apply(bikes).Count;
        BudgetTiles =
        [
            new("Under ₹1 lakh", "Commuters and city scooters", "/new-bikes?max=100000", Count(new BikeQuery { Max = 100_000 })),
            new("₹1 – 2 lakh", "Sporty everyday rides", "/new-bikes?min=100000&max=200000", Count(new BikeQuery { Min = 100_000, Max = 200_000 })),
            new("₹2 – 3 lakh", "Performance and touring", "/new-bikes?min=200000&max=300000", Count(new BikeQuery { Min = 200_000, Max = 300_000 })),
            new("Above ₹3 lakh", "Premium and superbikes", "/new-bikes?min=300000", Count(new BikeQuery { Min = 300_000 }))
        ];
        StyleTiles =
        [
            new("Motorcycles", "Commuter to superbike", "/new-bikes?type=bike", Count(new BikeQuery { Type = "bike" })),
            new("Scooters", "Easy, practical, automatic", "/new-bikes?type=scooter", Count(new BikeQuery { Type = "scooter" })),
            new("Electric", "Zero tailpipe, low running cost", "/new-bikes?fuel=electric", Count(new BikeQuery { Fuel = "electric" }))
        ];

        var byRange = Catalog.Brands.OrderByDescending(b => b.Bikes.Count).ThenBy(b => b.Name).ToList();
        TopBrands = byRange.Take(12).ToList();
        OtherBrands = byRange.Skip(12).OrderBy(b => b.Name).ToList();
        LatestLaunches = bikes.Where(b => b.LaunchDate is not null).OrderByDescending(b => b.LaunchDate).Take(10).ToList();
        ComparePicks = Editorial.ComparePairs.Take(6)
            .Select(p => new ComparePick(p.A, p.B, Compare.IndexModel.UrlFor([p.A, p.B]), CompareImage(p.Topic)))
            .ToList();

        LeadStory = Editorial.Blogs.FirstOrDefault();
        MoreStories = Editorial.Blogs.Skip(1).Take(2).ToList();
        News = Editorial.News.Take(4).ToList();
        SmartBuys = Editorial.ValueForMoney.Take(3).ToList();
        Radar = Editorial.Upcoming.Take(5).ToList();

        Meta.Title = "BikeCarHub – New Bikes, Scooters and EVs in India: Prices, Specs and News";
        Meta.Description = "Find new bikes, scooters and electric two-wheelers in India. Compare models, check ex-showroom prices and specifications, and read the latest news and reviews.";
        Meta.CanonicalPath = "/";
        if (Editorial.HeroSlides.Count > 0) Meta.PreloadImage = images.Resolve(Editorial.HeroSlides[0].ImagePath);
        Meta.JsonLd.Add(JsonLd.Organization(site, images.Resolve("/images/bchlogo/25-header.png")));
    }
}
