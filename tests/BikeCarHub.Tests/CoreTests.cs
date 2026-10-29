using BikeCarHub.Domain;
using BikeCarHub.Services;
using Xunit;

namespace BikeCarHub.Tests;

public class SluggerTests
{
    [Theory]
    [InlineData("Bajaj Pulsar NS200", "bajaj-pulsar-ns200")]
    [InlineData("  Honda Hornet 2.0 ", "honda-hornet-2-0")]
    [InlineData("Black & Blue", "black-and-blue")]
    [InlineData("Ducati Monster 30° Anniversario", "ducati-monster-30-anniversario")]
    [InlineData("best-indian-cars-under-₹8-lakh-on-road-price-(2024)", "best-indian-cars-under-8-lakh-on-road-price-2024")]
    [InlineData("new-royal-enfield-classic-350:price-expectation", "new-royal-enfield-classic-350-price-expectation")]
    [InlineData(null, "")]
    public void Slugify_produces_clean_lowercase_slugs(string? input, string expected) =>
        Assert.Equal(expected, Slugger.Slugify(input));

    [Theory]
    [InlineData("Bajaj Pulsar 150", "Bajaj", "Pulsar 150")]
    [InlineData("Ola Roadster", "Ola Electric", "Roadster")]
    [InlineData("F77", "Ultraviolette Automotive", "F77")]
    [InlineData("Bajaj", "Bajaj", "Bajaj")]
    public void StripBrand_removes_shared_leading_words(string name, string brand, string expected) =>
        Assert.Equal(expected, Slugger.StripBrand(name, brand));

    [Fact]
    public void Unique_appends_the_id_on_a_collision()
    {
        var used = new HashSet<string>();
        Assert.Equal("std", Slugger.Unique("std", 1, used));
        Assert.Equal("std-2", Slugger.Unique("std", 2, used));
    }
}

public class FormattingTests
{
    [Theory]
    [InlineData("Rs. 1,13,734 - 1,20,819", 113734, 120819)]
    [InlineData("Rs. 2,13,187", 213187, 213187)]
    public void ParsePriceRange_reads_rupee_text(string text, int min, int max) =>
        Assert.Equal((min, max), Formatting.ParsePriceRange(text));

    [Fact]
    public void ParsePriceRange_returns_nulls_when_there_is_no_price() =>
        Assert.Equal(((int?)null, (int?)null), Formatting.ParsePriceRange("Coming soon"));

    [Theory]
    [InlineData(75437, "₹75,437")]
    [InlineData(113734, "₹1,13,734")]
    [InlineData(10000000, "₹1,00,00,000")]
    public void Rupees_uses_indian_grouping(long amount, string expected) =>
        Assert.Equal(expected, Formatting.Rupees(amount));

    [Theory]
    [InlineData(75437, "₹75,437")]
    [InlineData(113734, "₹1.14 lakh")]
    [InlineData(7990000, "₹79.9 lakh")]
    [InlineData(12500000, "₹1.25 crore")]
    public void PriceShort_uses_lakh_and_crore(int amount, string expected) =>
        Assert.Equal(expected, Formatting.PriceShort(amount));

    [Fact]
    public void PriceRange_never_invents_a_price() =>
        Assert.Equal("Price not available", Formatting.PriceRange(null, null));

    [Theory]
    [InlineData("GroundClearance", "Ground clearance")]
    [InlineData("Milage", "Mileage")]
    [InlineData("LEDTailLight", "LED tail light")]
    [InlineData("ABS", "ABS")]
    public void Humanize_turns_column_names_into_labels(string column, string expected) =>
        Assert.Equal(expected, Formatting.Humanize(column));

    [Theory]
    [InlineData("24 Nov 2001", 2001)]
    [InlineData("2005", 2005)]
    public void ParseLaunchDate_accepts_the_stored_formats(string text, int year) =>
        Assert.Equal(year, Formatting.ParseLaunchDate(text)!.Value.Year);

    [Fact]
    public void ParseLaunchDate_rejects_free_text() => Assert.Null(Formatting.ParseLaunchDate("Coming soon"));
}

public class EmiCalculatorTests
{
    [Fact]
    public void Calculate_matches_the_worked_example()
    {
        // ₹1,00,000 at 10% for 3 years, as given in the EMI brief.
        var result = EmiCalculator.Calculate(100_000, 10, 3);
        Assert.Equal(3227, result.MonthlyEmi);
        Assert.Equal(16162, result.TotalInterest);
        Assert.Equal(116162, result.TotalPayable);
    }

    [Theory]
    [InlineData(49_999, 10, 3)]
    [InlineData(100_000, 19, 3)]
    [InlineData(100_000, 10, 8)]
    public void IsValid_rejects_values_outside_the_allowed_range(decimal amount, decimal rate, int years) =>
        Assert.False(EmiCalculator.IsValid(amount, rate, years));

    [Fact]
    public void Schedule_repays_the_whole_loan_and_matches_the_total_interest()
    {
        var rows = EmiCalculator.Schedule(100_000, 10, 3);
        Assert.Equal(3, rows.Count);
        Assert.Equal(0, rows[^1].Balance);
        Assert.InRange(rows.Sum(r => r.PrincipalPaid), 99_998, 100_002);
        Assert.InRange(rows.Sum(r => r.InterestPaid), 16_160, 16_164);
        Assert.True(rows[0].InterestPaid > rows[^1].InterestPaid);
    }

    [Fact]
    public void ProcessingFee_is_a_rounded_percentage_of_the_loan() =>
        Assert.Equal(1_500, EmiCalculator.ProcessingFee(100_000, 1.5m));

    [Theory]
    [InlineData(-0.5)]
    [InlineData(5.5)]
    public void IsValidFee_rejects_values_outside_the_allowed_range(decimal fee) =>
        Assert.False(EmiCalculator.IsValidFee(fee));
}

public class EvRunningCostTests
{
    private static readonly RunningCostInput Commute = new(30, 26, 8, 153, 10, 100, 50);

    [Fact]
    public void Calculate_works_out_the_charging_and_petrol_costs()
    {
        // 3.5 kWh ÷ 0.9 × ₹8 = ₹31.11 a charge; ÷ 153 km = ₹0.20/km; × 780 km = ₹159.
        var result = RunningCost.Calculate(3.5m, Commute);
        Assert.Equal(780, result.KmPerMonth);
        Assert.Equal(31, result.EvCostPerCharge);
        Assert.Equal(0.20m, result.EvCostPerKm);
        Assert.Equal(159, result.EvMonthly);
        Assert.Equal(1560, result.PetrolMonthly);
        Assert.Equal(1401, result.MonthlySaving);
    }

    [Fact]
    public void PaybackMonths_rounds_up_and_handles_no_saving()
    {
        Assert.Equal(36, RunningCost.PaybackMonths(150_000, 100_000, 1_400));
        Assert.Equal(0, RunningCost.PaybackMonths(90_000, 100_000, 1_400));
        Assert.Null(RunningCost.PaybackMonths(150_000, 100_000, -10));
    }

    [Fact]
    public void IsValid_rejects_values_outside_the_allowed_range() =>
        Assert.False(RunningCost.IsValid(Commute with { RangeKm = 10 }));

    [Fact]
    public void Facts_are_read_from_spec_labels_and_battery_needs_a_plausible_kwh()
    {
        var facts = EvFacts.From([
            new SpecSection("Range", [new SpecItem("Claimed range", "153 km/charge")]),
            new SpecSection("Powertrain", [new SpecItem("Range eco mode", "123 km"), new SpecItem("Range normal mode", "105 km"), new SpecItem("Range sports mode", "153 km")]),
            new SpecSection("Motor and battery", [new SpecItem("Battery capacity", "3.5")]),
            new SpecSection("Charging", [new SpecItem("Charging time 0–80%", "2 hr 55 min")])
        ]);
        Assert.Equal(3.5m, facts.BatteryKwh);
        Assert.Equal(153, facts.ClaimedRangeKm);
        Assert.Equal("2 hr 55 min", facts.ChargeTo80);
        Assert.True(facts.CanCost);
        // Sport mode repeats the claimed distance, so it is not offered twice.
        Assert.Equal(["Claimed", "Eco mode", "Normal mode"], facts.RangeOptions.Select(o => o.Label));

        var wh = EvFacts.From([new SpecSection("Battery", [new SpecItem("Battery capacity", "3500")])]);
        Assert.Null(wh.BatteryKwh);
    }
}

public class UpcomingStatusTests
{
    private static readonly DateTime Today = new(2026, 10, 13);

    [Fact]
    public void A_past_expected_date_is_not_upcoming() =>
        Assert.Equal(LaunchStatus.DatePassed, new UpcomingBike { ExpectedDate = new DateTime(2025, 12, 17) }.StatusOn(Today));

    [Fact]
    public void A_future_expected_date_is_upcoming() =>
        Assert.Equal(LaunchStatus.Upcoming, new UpcomingBike { ExpectedDate = new DateTime(2027, 1, 1) }.StatusOn(Today));

    [Fact]
    public void A_missing_date_is_not_upcoming() =>
        Assert.Equal(LaunchStatus.DatePassed, new UpcomingBike().StatusOn(Today));
}
