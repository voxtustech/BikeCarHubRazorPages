using BikeCarHub.Data;
using BikeCarHub.Domain;

namespace BikeCarHub.Services;

public interface ISpecService
{
    Task<IReadOnlyList<SpecSection>> GetSectionsAsync(Variant variant);
}

public sealed class SpecService(ICatalogRepository repo, ICacheStore cache) : ISpecService
{
    private static readonly (string Title, string Table)[] PetrolSections =
    [
        ("Key specifications", "TWSpec"),
        ("Mileage and performance", "TWMileageAndPerformances"),
        ("Engine and transmission", "TWEngineAndTransmissions"),
        ("Features", "TWFeatures"),
        ("Safety", "TWSafety"),
        ("Dimensions and capacity", "TWDimensionsAndCapacities"),
        ("Electricals", "TWElectricals"),
        ("Tyres and brakes", "TWTyresAndBrakes"),
        ("Motor and battery", "TWMotorAndBatteries"),
        ("Underpinnings", "TWUnderpinnings"),
        ("Charging", "TWChargings"),
        ("Chassis and suspension", "TWChassisAndSuspensions")
    ];

    private static readonly (string Title, string Table)[] ElectricSections =
    [
        ("Range", "TWEVRanges"),
        ("Performance", "TWEVPerformances"),
        ("Motor and battery", "TWEVMotorAndBatteries"),
        ("Charging", "TWEVChargings"),
        ("Powertrain", "TWEVEngineAndTransmissions"),
        ("Features", "TWEVFeatures"),
        ("App features", "TWEVAppFeatures"),
        ("Safety", "TWEVSafety"),
        ("Dimensions and capacity", "TWEVDimensionsAndCapacities"),
        ("Electricals", "TWEVElectricals"),
        ("Tyres and brakes", "TWEVTyresAndBrakes"),
        ("Underpinnings", "TWEVUnderpinnings"),
        ("Chassis and suspension", "TWEVChassisAndSuspensions")
    ];

    // Units are added only to numeric columns whose unit is not in doubt.
    private static readonly Dictionary<string, string> Units = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Displacement"] = "cc",
        ["Milage"] = "km/l", ["OverallMileage"] = "km/l", ["CityMileage"] = "km/l", ["HighwayMileage"] = "km/l",
        ["FuelCapacity"] = "L", ["FuelReserve"] = "L",
        ["Width"] = "mm", ["Length"] = "mm", ["Height"] = "mm", ["GroundClearance"] = "mm", ["Wheelbase"] = "mm",
        ["FrontBrakeDiameter"] = "mm", ["RearBrakeDiameter"] = "mm",
        ["KerbWeight"] = "kg", ["TopSpeed"] = "km/h"
    };

    private static readonly HashSet<string> HiddenColumns = new(StringComparer.OrdinalIgnoreCase)
        { "TWName", "EVName", "Varients", "CreatedDateTime" };

    private static readonly HashSet<string> EmptyValues = new(StringComparer.OrdinalIgnoreCase) { "", "NULL", "-", "N/A", "NA" };

    public Task<IReadOnlyList<SpecSection>> GetSectionsAsync(Variant variant) =>
        cache.GetOrCreateAsync($"specs:{variant.Id}", TimeSpan.FromMinutes(30), () => LoadAsync(variant));

    private async Task<IReadOnlyList<SpecSection>> LoadAsync(Variant variant)
    {
        var sections = variant.Bike.IsEv ? ElectricSections : PetrolSections;
        var rows = await repo.GetSpecRowsAsync(sections.Select(s => s.Table).ToList(), variant.Id);
        var result = new List<SpecSection>();
        for (var i = 0; i < sections.Length; i++)
        {
            if (rows[i] is not { } row) continue;
            var items = new List<SpecItem>();
            foreach (var (column, raw) in row)
            {
                if (HiddenColumns.Contains(column) || column.EndsWith("Id", StringComparison.OrdinalIgnoreCase)) continue;
                var value = Format(column, raw);
                if (value is not null) items.Add(new SpecItem(Formatting.Humanize(column), value));
            }
            // Empty modules are hidden rather than shown as a wall of "Not available".
            if (items.Count > 0) result.Add(new SpecSection(sections[i].Title, items));
        }
        return result;
    }

    private static string? Format(string column, object? raw)
    {
        switch (raw)
        {
            case null or DBNull:
                return null;
            case bool b:
                return b ? "Yes" : "No";
            case decimal or int or long or double or float:
                var number = Convert.ToDecimal(raw);
                if (number == 0) return null;
                var text = Formatting.Number(number);
                return Units.TryGetValue(column, out var unit) ? $"{text} {unit}" : text;
            default:
                var s = raw.ToString()?.Trim() ?? "";
                return EmptyValues.Contains(s) ? null : s;
        }
    }
}
