using BikeCarHub.Domain;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Electric;

/// <summary>The electric section covers every model in <see cref="IElectricCatalog"/>.</summary>
public static class ElectricSection
{
    /// <summary>While true, the section stays out of search results.</summary>
    public const bool IsPilot = true;

    public static string UrlFor(Bike bike) => $"/electric-bikes/{bike.Brand.Slug}/{bike.Slug}";
}

public class RunningCostModel(ICatalogService catalogService, IElectricCatalog electricCatalog, IImageUrlProvider images) : BasePageModel
{
    public const decimal DefaultKmPerDay = 30, DefaultTariff = 8, DefaultLoss = 10, DefaultPetrol = 100;
    public const int DefaultDays = 26;
    public const int CommuterCc = 125;

    public Bike Bike { get; private set; } = null!;
    public EvFacts Facts { get; private set; } = null!;
    public IReadOnlyList<Bike> PetrolOptions { get; private set; } = [];
    public Bike? Petrol { get; private set; }
    /// <summary>Why the pre-selected petrol model was chosen, shown under the picker.</summary>
    public string DefaultNote { get; private set; } = "";
    public RunningCostInput Input { get; private set; } = null!;
    public RunningCostResult? Result { get; private set; }
    public int? PaybackMonths { get; private set; }
    public bool OutOfRange { get; private set; }

    public async Task<IActionResult> OnGetAsync(string brand, string model, decimal? km, int? days, decimal? tariff,
        decimal? range, decimal? loss, decimal? petrol, string? vs)
    {
        var catalog = await catalogService.GetAsync();
        if (catalog.FindBike(brand, model) is not { } bike || await electricCatalog.FindAsync(bike) is not { } electric) return NotFound();
        Bike = bike;
        Facts = electric.Facts;

        // Petrol models of the same body type with a claimed mileage, so the comparison is like for like.
        PetrolOptions = catalog.Bikes
            .Where(b => !b.IsEv && b.IsScooter == bike.IsScooter && b.MileageKmpl > 0 && b.MinPrice is not null)
            .OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        // Scooters default to the everyday alternative: a commuter-sized (up to 125cc) petrol scooter closest in price.
        // Motorcycles range from commuters to performance bikes, so price alone picks the same class.
        // With no listed price, the entry-level commuter is the cautious baseline.
        var commuters = PetrolOptions.Where(b => b.DisplacementCc is <= CommuterCc).ToList();
        var pool = (bike.IsScooter || bike.MinPrice is null) && commuters.Count > 0 ? commuters : PetrolOptions;
        Petrol = PetrolOptions.FirstOrDefault(b => b.FullSlug == vs)
            ?? (bike.MinPrice is { } price ? pool.OrderBy(b => Math.Abs(price - b.MinPrice!.Value)) : pool.OrderBy(b => b.MinPrice)).FirstOrDefault();
        var kind = bike.IsScooter ? "scooter" : "motorcycle";
        DefaultNote = bike.MinPrice is null ? $"the lowest-priced petrol {kind} up to {CommuterCc}cc, as this model has no listed price."
            : bike.IsScooter ? $"the petrol scooter up to {CommuterCc}cc closest in price." : "the petrol motorcycle closest in price.";
        if (vs is not null && Petrol?.FullSlug != vs) OutOfRange = true;

        var defaults = new RunningCostInput(DefaultKmPerDay, DefaultDays, DefaultTariff, Facts.ClaimedRangeKm ?? 100, DefaultLoss, DefaultPetrol, Petrol?.MileageKmpl);
        var requested = new RunningCostInput(km ?? defaults.KmPerDay, days ?? defaults.DaysPerMonth, tariff ?? defaults.TariffPerKwh,
            range ?? defaults.RangeKm, loss ?? defaults.ChargingLossPercent, petrol ?? defaults.PetrolPerLitre, Petrol?.MileageKmpl);
        if (RunningCost.IsValid(requested)) Input = requested;
        else (Input, OutOfRange) = (defaults, true);

        if (Facts.CanCost)
        {
            Result = RunningCost.Calculate(Facts.BatteryKwh!.Value, Input);
            PaybackMonths = RunningCost.PaybackMonths(bike.MinPrice, Petrol?.MinPrice, Result.MonthlySaving);
        }

        var image = images.Resolve(bike.ImagePath);
        Meta.Title = $"{bike.Name} Running Cost, Range and Charging | BikeCarHub";
        Meta.Description = $"What it costs to charge and ride the {bike.Name} each month"
            + (Facts.ClaimedRangeKm is { } r ? $", with its {Formatting.Number(r)} km claimed range" : "")
            + ", and how that compares with a petrol " + (bike.IsScooter ? "scooter." : "bike.");
        Meta.CanonicalPath = ElectricSection.UrlFor(bike);
        Meta.NoIndex = ElectricSection.IsPilot || Request.Query.Count > 0;
        Meta.Image = image;
        Meta.PreloadImage = image;
        Meta.Crumbs(("Electric bikes", "/electric-bikes"), (bike.Name, ElectricSection.UrlFor(bike)));
        return Page();
    }
}
