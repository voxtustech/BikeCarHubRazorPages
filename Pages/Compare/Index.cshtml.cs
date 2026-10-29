using BikeCarHub.Domain;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Compare;

public class IndexModel(ICatalogService catalogService, ISpecService specService, IContentService contentService) : BasePageModel
{
    public sealed record Column(Bike Bike, Variant? Variant, IReadOnlyList<SpecSection> Sections);
    public sealed record Row(string Label, IReadOnlyList<string?> Values);
    public sealed record Group(string Title, IReadOnlyList<Row> Rows);

    public Catalog Catalog { get; private set; } = null!;
    public IReadOnlyList<Column> Columns { get; private set; } = [];
    public IReadOnlyList<Group> Groups { get; private set; } = [];
    public IReadOnlyList<(Bike A, Bike B)> Popular { get; private set; } = [];
    public string?[] Preselected { get; private set; } = new string?[3];

    /// <summary>Bikes are ordered by slug so each pair has exactly one address.</summary>
    public static string UrlFor(IReadOnlyList<Bike> bikes) =>
        "/compare/" + string.Join("-vs-", bikes.Select(b => b.FullSlug).OrderBy(s => s, StringComparer.Ordinal));

    public async Task<IActionResult> OnGetAsync(string? slug, string? a, string? v1, string? v2, string? v3)
    {
        Catalog = await catalogService.GetAsync();

        if (slug is null)
        {
            Popular = (await contentService.GetAsync()).ComparePairs.Select(p => (p.A, p.B)).ToList();
            Preselected[0] = a is not null && Catalog.BikeByFullSlug.ContainsKey(a) ? a : null;
            Meta.Title = "Compare Bikes – Side-by-Side Specs and Prices | BikeCarHub";
            Meta.Description = "Compare two or three bikes or scooters side by side: ex-showroom price, engine, mileage, features, dimensions and more.";
            Meta.CanonicalPath = "/compare";
            Meta.NoIndex = a is not null;
            Meta.Crumbs(("Compare", "/compare"));
            return Page();
        }

        var parts = slug.Split("-vs-", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 2 or > 3) return NotFound();
        var bikes = new List<Bike>();
        foreach (var part in parts)
        {
            if (!Catalog.BikeByFullSlug.TryGetValue(part, out var bike) || bikes.Contains(bike)) return NotFound();
            bikes.Add(bike);
        }

        var canonical = UrlFor(bikes);
        if (!string.Equals(canonical, $"/compare/{slug}", StringComparison.Ordinal))
            return Moved(canonical + Request.QueryString);
        bikes = bikes.OrderBy(b => b.FullSlug, StringComparer.Ordinal).ToList();

        string?[] chosen = [v1, v2, v3];
        var columns = new List<Column>();
        for (var i = 0; i < bikes.Count; i++)
        {
            var variant = bikes[i].Variants.FirstOrDefault(v => v.Slug == chosen[i]) ?? bikes[i].Variants.FirstOrDefault();
            IReadOnlyList<SpecSection> sections = variant is null ? [] : await specService.GetSectionsAsync(variant);
            columns.Add(new Column(bikes[i], variant, sections));
        }
        Columns = columns;
        for (var i = 0; i < bikes.Count; i++) Preselected[i] = bikes[i].FullSlug;

        // Rows are the union of every label, in first-seen order, so nothing is dropped when bikes differ.
        var groups = new List<Group>();
        foreach (var title in columns.SelectMany(c => c.Sections.Select(s => s.Title)).Distinct())
        {
            var labels = columns.SelectMany(c => c.Sections.Where(s => s.Title == title).SelectMany(s => s.Items.Select(i => i.Label))).Distinct();
            var rows = labels.Select(label => new Row(label, columns
                .Select(c => c.Sections.FirstOrDefault(s => s.Title == title)?.Items.FirstOrDefault(i => i.Label == label)?.Value)
                .ToList())).ToList();
            groups.Add(new Group(title, rows));
        }
        Groups = groups;

        var names = string.Join(" vs ", bikes.Select(b => b.Name));
        Meta.Title = $"{names} – Comparison | BikeCarHub";
        Meta.Description = $"Compare {names}: ex-showroom price, engine, mileage, features, brakes and dimensions side by side.";
        Meta.CanonicalPath = canonical;
        Meta.NoIndex = Request.Query.Count > 0;
        Meta.Crumbs(("Compare", "/compare"), (names, canonical));
        return Page();
    }
}
