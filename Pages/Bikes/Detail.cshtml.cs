using BikeCarHub.Domain;
using BikeCarHub.Seo;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Bikes;

public class DetailModel(ICatalogService catalogService, ISpecService specService, IElectricCatalog electricCatalog, IImageUrlProvider images, SiteSettings site) : BasePageModel
{
    public Bike Bike { get; private set; } = null!;
    /// <summary>The variant whose specifications are shown; null when the model has no variants.</summary>
    public Variant? Selected { get; private set; }
    public bool IsVariantPage { get; private set; }
    public IReadOnlyList<SpecSection> Sections { get; private set; } = [];
    public IReadOnlyList<Bike> Similar { get; private set; } = [];
    /// <summary>Set for electric models that have a running cost calculator.</summary>
    public string? RunningCostUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync(string brand, string model, string? variant)
    {
        var catalog = await catalogService.GetAsync();
        var bike = catalog.FindBike(brand, model);
        if (bike is null)
        {
            if (catalog.RemovedKeys.Contains($"{brand}/{model}")) return Gone();
            return FindRenamed(catalog, brand, model) is { } target ? Moved(target.Url) : NotFound();
        }

        Bike = bike;
        if (variant is not null)
        {
            Selected = bike.Variants.FirstOrDefault(v => v.Slug == variant);
            if (Selected is null)
            {
                var wanted = Slugger.Slugify(variant);
                var match = bike.Variants.FirstOrDefault(v => v.Slug == wanted || Slugger.Slugify(v.Name) == wanted);
                return match is not null ? Moved(match.Url) : NotFound();
            }
            IsVariantPage = true;
        }
        else
        {
            Selected = bike.Variants.FirstOrDefault();
        }

        if (Selected is not null) Sections = await specService.GetSectionsAsync(Selected);
        if (await electricCatalog.FindAsync(bike) is not null) RunningCostUrl = Pages.Electric.ElectricSection.UrlFor(bike);

        Similar = catalog.Bikes
            .Where(b => b != bike && b.IsScooter == bike.IsScooter && b.IsEv == bike.IsEv)
            .OrderBy(b => bike.MinPrice is { } p && b.MinPrice is { } q ? Math.Abs(p - q) : int.MaxValue)
            .ThenBy(b => b.Brand == bike.Brand ? 0 : 1)
            .Take(4)
            .ToList();

        var image = images.Resolve(bike.ImagePath);
        var price = bike.MinPrice is null ? "" : $" Ex-showroom price {Formatting.PriceRange(bike.MinPrice, bike.MaxPrice)}.";
        if (IsVariantPage)
        {
            Meta.Title = Selected!.SeoTitle ?? $"{Selected.Name} – Specifications and Features | BikeCarHub";
            Meta.Description = Selected.SeoDescription
                ?? $"{Selected.Name} specifications and features: engine, mileage, brakes, dimensions and more.{price}";
            Meta.CanonicalPath = Selected.Url;
            Meta.Crumbs(("New bikes", "/new-bikes"), (bike.Brand.Name, bike.Brand.Url), (bike.ShortName, bike.Url), (Selected.ShortName, Selected.Url));
        }
        else
        {
            Meta.Title = $"{bike.Name} – Price, Specifications and Variants | BikeCarHub";
            Meta.Description = $"{bike.Name} price, specifications, features and {bike.Variants.Count} {(bike.Variants.Count == 1 ? "variant" : "variants")}.{price}";
            Meta.CanonicalPath = bike.Url;
            Meta.Crumbs(("New bikes", "/new-bikes"), (bike.Brand.Name, bike.Brand.Url), (bike.ShortName, bike.Url));
        }
        Meta.OgType = "product";
        Meta.Image = image;
        Meta.PreloadImage = image;
        Meta.JsonLd.Add(JsonLd.ForProduct(site, bike, IsVariantPage ? Selected : null, image, includeOffer: !IsVariantPage));
        return Page();
    }

    /// <summary>Finds the model a mistyped or older address was meant for.</summary>
    private static Bike? FindRenamed(Catalog catalog, string brand, string model)
    {
        var brandSlug = Slugger.Slugify(brand);
        var modelSlug = Slugger.Slugify(model);
        if (!catalog.BrandBySlug.TryGetValue(brandSlug, out var found)) return null;
        return found.Bikes.FirstOrDefault(b => b.Slug == modelSlug)
            ?? found.Bikes.FirstOrDefault(b => b.FullSlug == modelSlug || Slugger.Slugify(b.Name) == modelSlug);
    }
}
