using System.Globalization;
using System.Text.RegularExpressions;
using BikeCarHub.Domain;

namespace BikeCarHub.Services;

/// <summary>The electric figures a buyer asks about first, read from the variant's spec sections.</summary>
public sealed partial record EvFacts(
    decimal? BatteryKwh, decimal? ClaimedRangeKm, decimal? EcoRangeKm, decimal? NormalRangeKm, decimal? SportRangeKm, decimal? TopSpeedKmh,
    string? ChargeTo80, string? ChargeTo100, string? ChargerOutput, string? BatteryWarranty, bool? HomeCharging)
{
    /// <summary>A running cost needs both the battery size and a range.</summary>
    public bool CanCost => BatteryKwh is not null && ClaimedRangeKm is not null;

    /// <summary>Ranges a rider can pick from, claimed first, without repeating the same distance.</summary>
    public IReadOnlyList<(string Label, decimal Km)> RangeOptions =>
        new (string, decimal?)[] { ("Claimed", ClaimedRangeKm), ("Eco mode", EcoRangeKm), ("Normal mode", NormalRangeKm), ("Sport mode", SportRangeKm) }
            .Where(o => o.Item2 is not null)
            .Select(o => (o.Item1, o.Item2!.Value))
            .DistinctBy(o => o.Item2)
            .ToList();

    public static EvFacts From(IReadOnlyList<SpecSection> sections)
    {
        var items = sections.SelectMany(s => s.Items).GroupBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);
        string? Text(string label) => items.TryGetValue(label, out var v) ? v : null;

        // Battery capacity is stored without a unit. Values in this range are kWh; anything else is not trusted.
        var battery = Number(Text("Battery capacity")) is { } kwh && kwh is >= 0.5m and <= 20m ? kwh : (decimal?)null;
        return new EvFacts(
            battery,
            Positive(Number(Text("Claimed range"))),
            Positive(Number(Text("Range eco mode"))),
            Positive(Number(Text("Range normal mode"))),
            Positive(Number(Text("Range sports mode"))),
            Positive(Number(Text("Top speed"))),
            ChargeTime(items, "80%"),
            ChargeTime(items, "100%"),
            Text("Charger output"),
            Text("Battery warranty"),
            Text("Charging at home") is { } home ? home == "Yes" : null);
    }

    // Charging labels carry an en dash ("0–80%"), so they are matched by their start and end.
    private static string? ChargeTime(Dictionary<string, string> items, string to) =>
        items.FirstOrDefault(i => i.Key.StartsWith("Charging time", StringComparison.OrdinalIgnoreCase) && i.Key.EndsWith(to, StringComparison.Ordinal)).Value;

    private static decimal? Positive(decimal? value) => value > 0 ? value : null;

    /// <summary>First number in a value such as "153 km/charge".</summary>
    internal static decimal? Number(string? text) =>
        text is not null && NumberPattern().Match(text) is { Success: true } m
            ? decimal.Parse(m.Value.Replace(",", ""), CultureInfo.InvariantCulture)
            : null;

    [GeneratedRegex(@"\d[\d,]*(\.\d+)?")]
    private static partial Regex NumberPattern();
}

public sealed record RunningCostInput(
    decimal KmPerDay, int DaysPerMonth, decimal TariffPerKwh, decimal RangeKm, decimal ChargingLossPercent,
    decimal PetrolPerLitre, decimal? PetrolKmpl);

public sealed record RunningCostResult(
    decimal KmPerMonth, decimal EvCostPerKm, decimal EvCostPerCharge, decimal ChargesPerMonth, decimal EvMonthly,
    decimal? PetrolCostPerKm, decimal? PetrolMonthly, decimal? MonthlySaving);

public static class RunningCost
{
    public const int MinKmPerDay = 1, MaxKmPerDay = 300;
    public const int MinDays = 1, MaxDays = 31;
    public const decimal MinTariff = 1, MaxTariff = 30;
    public const int MinRange = 20, MaxRange = 500;
    public const decimal MaxLoss = 30;
    public const decimal MinPetrol = 50, MaxPetrol = 200;

    public static bool IsValid(RunningCostInput i) =>
        i.KmPerDay is >= MinKmPerDay and <= MaxKmPerDay && i.DaysPerMonth is >= MinDays and <= MaxDays
        && i.TariffPerKwh is >= MinTariff and <= MaxTariff && i.RangeKm is >= MinRange and <= MaxRange
        && i.ChargingLossPercent is >= 0 and <= MaxLoss && i.PetrolPerLitre is >= MinPetrol and <= MaxPetrol;

    /// <summary>
    /// A full charge draws the battery's capacity from the wall plus what the charger loses as heat.
    /// The range divides that cost over the kilometres one charge actually gives.
    /// </summary>
    public static RunningCostResult Calculate(decimal batteryKwh, RunningCostInput i)
    {
        var kmPerMonth = i.KmPerDay * i.DaysPerMonth;
        var perCharge = batteryKwh / (1 - i.ChargingLossPercent / 100) * i.TariffPerKwh;
        var evPerKm = perCharge / i.RangeKm;
        var evMonthly = evPerKm * kmPerMonth;
        decimal? petrolPerKm = i.PetrolKmpl > 0 ? i.PetrolPerLitre / i.PetrolKmpl : null;
        decimal? petrolMonthly = petrolPerKm * kmPerMonth;
        return new RunningCostResult(
            kmPerMonth, Math.Round(evPerKm, 2), Math.Round(perCharge), Math.Round(kmPerMonth / i.RangeKm, 1), Math.Round(evMonthly),
            petrolPerKm is { } p ? Math.Round(p, 2) : null,
            petrolMonthly is { } m ? Math.Round(m) : null,
            petrolMonthly is { } pm ? Math.Round(pm - evMonthly) : null);
    }

    /// <summary>Months of fuel savings needed to cover the higher purchase price; null when it never pays back.</summary>
    public static int? PaybackMonths(int? evPrice, int? petrolPrice, decimal? monthlySaving) =>
        evPrice is { } ev && petrolPrice is { } petrol && monthlySaving > 0
            ? ev <= petrol ? 0 : (int)Math.Ceiling((ev - petrol) / monthlySaving.Value)
            : null;
}
