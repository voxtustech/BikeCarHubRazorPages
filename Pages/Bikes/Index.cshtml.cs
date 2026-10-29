using BikeCarHub.Domain;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Bikes;

public class IndexModel(ICatalogService catalogService) : BasePageModel
{
    public const int PageSize = 24;

    public Catalog Catalog { get; private set; } = null!;
    public BikeQuery Filter { get; private set; } = new();
    public IReadOnlyList<Bike> Results { get; private set; } = [];
    public int Total { get; private set; }
    public int PageNumber { get; private set; }
    public int TotalPages { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? q, string? brand, string? type, string? fuel, int? min, int? max, string? sort,
        [FromQuery(Name = "page")] int pageNumber = 1)
    {
        Catalog = await catalogService.GetAsync();
        Filter = new BikeQuery
        {
            Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim()[..Math.Min(q.Trim().Length, 60)],
            Brand = Catalog.BrandBySlug.ContainsKey(brand ?? "") ? brand : null,
            Type = type is "bike" or "scooter" ? type : null,
            Fuel = fuel is "electric" or "petrol" ? fuel : null,
            Min = min > 0 ? min : null,
            Max = max > 0 ? max : null,
            Sort = BikeQuery.SortOptions.Any(o => o.Key == sort) ? sort : null
        };

        var all = Filter.Apply(Catalog.Bikes);
        Total = all.Count;
        TotalPages = Math.Max(1, (Total + PageSize - 1) / PageSize);
        if (pageNumber < 1 || pageNumber > TotalPages) return NotFound();
        PageNumber = pageNumber;
        Results = all.Skip((pageNumber - 1) * PageSize).Take(PageSize).ToList();

        Meta.Title = pageNumber > 1
            ? $"New Bikes in India – Page {pageNumber} | BikeCarHub"
            : "New Bikes in India – Prices, Specs and Models | BikeCarHub";
        Meta.Description = $"Browse {Catalog.Bikes.Count} new bikes and scooters from {Catalog.Brands.Count} brands in India with ex-showroom prices, specifications and variants.";
        // Any filter or sort is a duplicate view of the clean list: keep it out of the index.
        Meta.NoIndex = Filter.HasFilters || Request.Query.Keys.Any(k => k != "page");
        Meta.CanonicalPath = Meta.NoIndex || pageNumber == 1 ? "/new-bikes" : $"/new-bikes?page={pageNumber}";
        Meta.Crumbs(("New bikes", "/new-bikes"));
        return Page();
    }
}
