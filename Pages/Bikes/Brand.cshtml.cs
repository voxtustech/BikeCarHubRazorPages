using BikeCarHub.Domain;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Bikes;

public class BrandModel(ICatalogService catalogService) : BasePageModel
{
    public Brand Brand { get; private set; } = null!;
    public string? Facet { get; private set; }
    public string Heading { get; private set; } = "";
    public BikeQuery Filter { get; private set; } = new();
    public IReadOnlyList<Bike> Results { get; private set; } = [];
    public IReadOnlyList<string> AvailableFacets { get; private set; } = [];
    public int PageNumber { get; private set; }
    public int TotalPages { get; private set; }

    public static string FacetLabel(string facet) => facet switch
    {
        "bikes" => "Bikes", "scooters" => "Scooters", "electric" => "Electric", _ => "Petrol"
    };

    public async Task<IActionResult> OnGetAsync(string brand, string? facet, int? min, int? max, string? sort,
        [FromQuery(Name = "page")] int pageNumber = 1)
    {
        var catalog = await catalogService.GetAsync();
        if (!catalog.BrandBySlug.TryGetValue(brand, out var found))
        {
            // A differently written brand name still reaches the right page, permanently.
            var normalized = Slugger.Slugify(brand);
            return catalog.BrandBySlug.TryGetValue(normalized, out var match)
                ? Moved(facet is null ? match.Url : $"{match.Url}/{facet}")
                : NotFound();
        }

        Brand = found;
        Facet = facet;
        Filter = new BikeQuery
        {
            Type = facet switch { "bikes" => "bike", "scooters" => "scooter", _ => null },
            Fuel = facet is "electric" or "petrol" ? facet : null,
            Min = min > 0 ? min : null,
            Max = max > 0 ? max : null,
            Sort = BikeQuery.SortOptions.Any(o => o.Key == sort) ? sort : null
        };

        var facetOnly = new BikeQuery { Type = Filter.Type, Fuel = Filter.Fuel }.Apply(found.Bikes);
        if (facet is not null && facetOnly.Count == 0) return NotFound();

        var all = Filter.Apply(found.Bikes);
        TotalPages = Math.Max(1, (all.Count + IndexModel.PageSize - 1) / IndexModel.PageSize);
        if (pageNumber < 1 || pageNumber > TotalPages) return NotFound();
        PageNumber = pageNumber;
        Results = all.Skip((pageNumber - 1) * IndexModel.PageSize).Take(IndexModel.PageSize).ToList();

        AvailableFacets = FacetConstraint.Facets.Where(f => new BikeQuery
        {
            Type = f switch { "bikes" => "bike", "scooters" => "scooter", _ => null },
            Fuel = f is "electric" or "petrol" ? f : null
        }.Apply(found.Bikes).Count is var n && n > 0 && n < found.Bikes.Count).ToList();

        var basePath = facet is null ? found.Url : $"{found.Url}/{facet}";
        Heading = facet switch
        {
            null => $"{found.Name} bikes",
            "bikes" => $"{found.Name} motorcycles",
            "scooters" => $"{found.Name} scooters",
            "electric" => $"{found.Name} electric two-wheelers",
            _ => $"{found.Name} petrol two-wheelers"
        };

        Meta.Title = facet is null && found.TitleTag is not null && pageNumber == 1
            ? found.TitleTag
            : $"{Heading} in India – Prices and Models{(pageNumber > 1 ? $" – Page {pageNumber}" : "")} | BikeCarHub";
        Meta.Description = facet is null && found.MetaDescription is not null
            ? found.MetaDescription
            : $"See all {facetOnly.Count} {Heading} available in India with ex-showroom prices, specifications and variants on BikeCarHub.";
        Meta.NoIndex = min is not null || max is not null || sort is not null || Request.Query.Keys.Any(k => k != "page");
        Meta.CanonicalPath = Meta.NoIndex || pageNumber == 1 ? basePath : $"{basePath}?page={pageNumber}";
        if (facet is null) Meta.Crumbs(("New bikes", "/new-bikes"), (found.Name, found.Url));
        else Meta.Crumbs(("New bikes", "/new-bikes"), (found.Name, found.Url), (FacetLabel(facet), basePath));
        return Page();
    }
}
