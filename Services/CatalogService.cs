using BikeCarHub.Data;
using BikeCarHub.Domain;

namespace BikeCarHub.Services;

/// <summary>Immutable snapshot of brands, models and variants with their slugs resolved.</summary>
public sealed class Catalog
{
    public required IReadOnlyList<Brand> Brands { get; init; }
    /// <summary>Active models only.</summary>
    public required IReadOnlyList<Bike> Bikes { get; init; }
    public required IReadOnlyDictionary<string, Brand> BrandBySlug { get; init; }
    public required IReadOnlyDictionary<string, Bike> BikeByFullSlug { get; init; }
    /// <summary>Models switched off with IsActive = 0; their URLs answer 410 Gone.</summary>
    public required IReadOnlyCollection<string> RemovedKeys { get; init; }
    public required DateTime LoadedUtc { get; init; }

    public Bike? FindBike(string brandSlug, string modelSlug) =>
        BrandBySlug.TryGetValue(brandSlug, out var brand) ? brand.Bikes.FirstOrDefault(b => b.Slug == modelSlug) : null;

    public Bike? FindBikeByName(string name) =>
        Bikes.FirstOrDefault(b => string.Equals(b.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
}

public interface ICatalogService
{
    Task<Catalog> GetAsync();
}

public sealed class CatalogService(ICatalogRepository repo, ICacheStore cache, IImageUrlProvider images) : ICatalogService
{
    public Task<Catalog> GetAsync() => cache.GetOrCreateAsync("catalog", TimeSpan.FromMinutes(30), LoadAsync);

    private async Task<Catalog> LoadAsync()
    {
        var brandRows = await repo.GetBrandsAsync();
        var bikeRows = await repo.GetBikesAsync();
        var variantRows = await repo.GetVariantsAsync();
        var displacement = (await repo.GetDisplacementsAsync()).ToDictionary(r => r.TwoWheelerId, r => r.Value);
        var mileage = (await repo.GetMileagesAsync()).ToDictionary(r => r.TwoWheelerId, r => r.Value);

        var brandSlugs = new HashSet<string>();
        var brands = brandRows
            .Where(r => !string.IsNullOrWhiteSpace(r.BrandName))
            .OrderBy(r => r.TWBrandId)
            .Select(r => new Brand
            {
                Id = r.TWBrandId,
                Name = r.BrandName!.Trim(),
                Slug = Slugger.Unique(Slugger.Slugify(r.BrandName), r.TWBrandId, brandSlugs),
                LogoPath = r.BrandLogoURL,
                HasPetrol = r.IsPetrol == true,
                HasEv = r.IsEV == true,
                Description = Clean(r.Discription),
                TitleTag = Clean(r.TitleTag),
                MetaDescription = Clean(r.MetaDescription),
                Created = r.CreatedDateTime
            })
            .ToDictionary(b => b.Id);

        var bikesById = new Dictionary<int, Bike>();
        var removed = new List<string>();
        var modelSlugs = brands.Keys.ToDictionary(id => id, _ => new HashSet<string>());

        foreach (var r in bikeRows.OrderBy(r => r.TwoWheelerId))
        {
            if (string.IsNullOrWhiteSpace(r.TwoWheelerName) || r.TwoWBrandId is not { } brandId || !brands.TryGetValue(brandId, out var brand))
                continue;

            var name = r.TwoWheelerName.Trim();
            var shortName = Slugger.StripBrand(name, brand.Name);
            var slug = Slugger.Unique(Slugger.Slugify(shortName), r.TwoWheelerId, modelSlugs[brandId]);

            if (r.IsActive == false)
            {
                removed.Add($"{brand.Slug}/{slug}");
                continue;
            }

            // The text price is the fullest source; the numeric columns are blank for many models.
            var (min, max) = Formatting.ParsePriceRange(r.Price);
            if (min is null)
            {
                var values = new[] { r.BasePrice, r.TopPrice }.Where(v => v is > 0).Select(v => v!.Value).ToList();
                if (values.Count > 0) (min, max) = (values.Min(), values.Max());
            }

            var bike = new Bike
            {
                Id = r.TwoWheelerId,
                Name = name,
                ShortName = shortName,
                Slug = slug,
                Brand = brand,
                Type = Clean(r.Type),
                ImagePath = Clean(r.TWImage) ?? FindModelImage(name),
                MinPrice = min,
                MaxPrice = max,
                LaunchText = Clean(r.LaunchDate),
                LaunchDate = Formatting.ParseLaunchDate(r.LaunchDate),
                IsEv = r.IsEV == true,
                IsActive = true,
                Description = Clean(r.Discription),
                Created = r.CreatedDateTime,
                DisplacementCc = displacement.GetValueOrDefault(r.TwoWheelerId) is > 0 and var cc ? cc : null,
                MileageKmpl = mileage.GetValueOrDefault(r.TwoWheelerId) is > 0 and var kmpl ? kmpl : null
            };
            bikesById[bike.Id] = bike;
            brand.Bikes.Add(bike);
        }

        foreach (var group in variantRows.Where(v => v.TwoWheelerId is not null).GroupBy(v => v.TwoWheelerId!.Value))
        {
            if (!bikesById.TryGetValue(group.Key, out var bike)) continue;
            var used = new HashSet<string>();
            foreach (var r in group.OrderBy(v => v.TWVarientId))
            {
                if (string.IsNullOrWhiteSpace(r.Varients)) continue;
                var name = r.Varients.Trim();
                var shortName = Slugger.StripPrefix(name, bike.Name);
                var baseSlug = string.Equals(shortName, bike.Name, StringComparison.OrdinalIgnoreCase)
                    ? Slugger.Slugify(Slugger.StripBrand(name, bike.Brand.Name))
                    : Slugger.Slugify(shortName);
                bike.Variants.Add(new Variant
                {
                    Id = r.TWVarientId,
                    Name = name,
                    ShortName = shortName,
                    Slug = Slugger.Unique(baseSlug, r.TWVarientId, used),
                    Bike = bike,
                    Created = r.CreatedDateTime,
                    SeoTitle = Clean(r.SEO_Title),
                    SeoDescription = Clean(r.SEO_Description)
                });
            }
        }

        var activeBrands = brands.Values.Where(b => b.Bikes.Count > 0).OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var b in activeBrands) b.Bikes.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
        var bikes = activeBrands.SelectMany(b => b.Bikes).ToList();

        return new Catalog
        {
            Brands = activeBrands,
            Bikes = bikes,
            BrandBySlug = activeBrands.ToDictionary(b => b.Slug),
            BikeByFullSlug = bikes.GroupBy(b => b.FullSlug).ToDictionary(g => g.Key, g => g.First()),
            RemovedKeys = removed.ToHashSet(),
            LoadedUtc = DateTime.UtcNow
        };
    }

    private static readonly string[] ImageExtensions = [".webp", ".avif", ".png", ".jpg", ".jpeg"];

    // Models added without TWImage still get a photo once one is placed in a folder named after the model
    // (e.g. TwoWheelerImages\Yamaha\Yamaha YZF-R2\racing blue.webp), so no database edit is needed.
    private string? FindModelImage(string modelName)
    {
        var folder = images.FindFolder("/images/twowheeler/twowheelerimages/", Slugger.Slugify(modelName));
        if (folder is null) return null;
        var file = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        return file is null ? null : images.ToVirtualPath(file);
    }

    private static string? Clean(string? value)
    {
        // Stored text carries non-breaking spaces and doubled spaces.
        var v = value is null ? null : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrEmpty(v) || v.Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : v;
    }
}

/// <summary>Filter, sort and page the catalogue for listing and brand pages.</summary>
public sealed class BikeQuery
{
    public string? Q { get; set; }
    public string? Brand { get; set; }
    public string? Type { get; set; }
    public string? Fuel { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }
    public string? Sort { get; set; }

    public static readonly (string Key, string Label)[] SortOptions =
    [
        ("name", "Name A–Z"), ("price-asc", "Price: low to high"), ("price-desc", "Price: high to low"), ("newest", "Newest launch")
    ];

    public bool HasFilters => Q is not null || Brand is not null || Type is not null || Fuel is not null || Min is not null || Max is not null
        || (Sort is not null && Sort != "name");

    public IReadOnlyList<Bike> Apply(IEnumerable<Bike> source)
    {
        var q = source;
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var words = Q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            q = q.Where(b => words.All(w => b.Name.Contains(w, StringComparison.OrdinalIgnoreCase)));
        }
        if (!string.IsNullOrEmpty(Brand)) q = q.Where(b => b.Brand.Slug == Brand);
        q = Type switch
        {
            "scooter" => q.Where(b => b.IsScooter),
            "bike" => q.Where(b => !b.IsScooter),
            _ => q
        };
        q = Fuel switch
        {
            "electric" => q.Where(b => b.IsEv),
            "petrol" => q.Where(b => !b.IsEv),
            _ => q
        };
        if (Min is { } min) q = q.Where(b => (b.MaxPrice ?? b.MinPrice) >= min);
        if (Max is { } max) q = q.Where(b => b.MinPrice <= max);
        q = Sort switch
        {
            "price-asc" => q.OrderBy(b => b.MinPrice is null).ThenBy(b => b.MinPrice),
            "price-desc" => q.OrderBy(b => b.MinPrice is null).ThenByDescending(b => b.MinPrice),
            "newest" => q.OrderBy(b => b.LaunchDate is null).ThenByDescending(b => b.LaunchDate),
            _ => q.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
        };
        return q.ToList();
    }
}
