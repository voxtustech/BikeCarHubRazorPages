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

    private static readonly string[] Slugs =
    [
        "mahindra-be-formula-e-edition", "triumph-speed-t4-receives-18000-discount-for-limited-time",
        "maruti-suzuki-brezza-the-value-for-money-variant", "maruti-suzuki-swift-2025-the-value-for-money-variant",
        "tvs-xl-ev", "tata-punch", "tata-punch-ev"
    ];

    [Theory]
    [InlineData("tVS-xL-eV", "tvs-xl-ev")]
    [InlineData("mahindraBE-formulaE-edition", "mahindra-be-formula-e-edition")]
    [InlineData("triumph-speed-t4-receives-₹18,000-discount-for-limited-time", "triumph-speed-t4-receives-18000-discount-for-limited-time")]
    [InlineData("marutisuzukibrezza", "maruti-suzuki-brezza-the-value-for-money-variant")]
    [InlineData("marutisuzukiswift2025", "maruti-suzuki-swift-2025-the-value-for-money-variant")]
    [InlineData("tata-punch", "tata-punch")]
    [InlineData("hero-splendor-plus", null)]
    [InlineData("tata", null)]
    [InlineData("marutisuzuki", null)]
    public void Match_finds_the_page_an_old_slug_meant(string wanted, string? expected) =>
        Assert.Equal(expected, Slugger.Match(Slugs, wanted, s => s));
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

public class RichDocxReaderTests
{
    private const string Ns = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    private static string P(string text, string? style = null, bool bold = false, bool list = false) =>
        "<w:p><w:pPr>" + (style is null ? "" : $"<w:pStyle w:val=\"{style}\"/>") + (list ? "<w:numPr><w:ilvl w:val=\"0\"/><w:numId w:val=\"1\"/></w:numPr>" : "") +
        "</w:pPr><w:r>" + (bold ? "<w:rPr><w:b/></w:rPr>" : "") + $"<w:t xml:space=\"preserve\">{System.Security.SecurityElement.Escape(text)}</w:t></w:r></w:p>";

    private static string Cell(params string[] paras) => "<w:tc>" + string.Concat(paras.Select(t => P(t))) + "</w:tc>";
    private static string Table(params string[][] rows) => "<w:tbl>" + string.Concat(rows.Select(r => "<w:tr>" + string.Concat(r) + "</w:tr>")) + "</w:tbl>";

    private static string WriteDocx(string bodyXml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"bch-test-{Guid.NewGuid():N}.docx");
        using (var zip = System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create))
        {
            using var w = new StreamWriter(zip.CreateEntry("word/document.xml").Open());
            w.Write($"<?xml version=\"1.0\"?><w:document xmlns:w=\"{Ns}\"><w:body>{bodyXml}</w:body></w:document>");
        }
        return path;
    }

    [Fact]
    public void Read_lays_out_a_word_article()
    {
        var path = WriteDocx(string.Concat(
            P("BIKECARHUB · NEW BIKES", bold: true),
            P("Test Bike 750: Specs", "Heading1"),
            P("A short intro <with> symbols."),
            P("At a glance", "Heading2"),
            Table([Cell("743 cc", "Twin engine"), Cell("55 PS", "@ 7,000 rpm"), Cell("63 Nm", "@ 5,000 rpm")]),
            P("Engine: twin", list: true),
            P("History", "Heading2"),
            Table([Cell("“A quote.”", "Maker")]),
            Table([Cell("Price not announced yet", "We will update this post.")]),
            P("Specifications", "Heading2"),
            Table([Cell("Specification"), Cell("Value")], [Cell("Displacement"), Cell("743 cc")]),
            P("Frequently asked questions", "Heading2"),
            P("Q.  How big is the engine?", bold: true),
            P("743cc.")));
        try
        {
            var doc = RichDocxReader.Read(path, _ => null, "/images/default.webp")!;
            Assert.Equal("Test Bike 750: Specs", doc.Title);
            Assert.Equal("A short intro <with> symbols.", doc.Standfirst);
            Assert.Equal("BIKECARHUB · NEW BIKES", doc.Kicker);
            Assert.DoesNotContain("BIKECARHUB", doc.BodyHtml);
            Assert.DoesNotContain("<with>", doc.BodyHtml);
            Assert.Contains("<h2 id=\"at-a-glance\">At a glance</h2>", doc.BodyHtml);
            Assert.Contains("<li><strong>743 cc</strong><span>Twin engine</span></li>", doc.BodyHtml);
            Assert.Contains("<ul class=\"square-list\"><li>Engine: twin</li></ul>", doc.BodyHtml);
            Assert.Contains("<figure class=\"pull-quote\"><blockquote><p>“A quote.”</p></blockquote><figcaption>Maker</figcaption>", doc.BodyHtml);
            Assert.Contains("<p class=\"callout-title\">Price not announced yet</p>", doc.BodyHtml);
            Assert.Contains("<th scope=\"row\">Displacement</th><td>743 cc</td>", doc.BodyHtml);
            Assert.Contains("How big is the engine?</h3><p>743cc.</p>", doc.BodyHtml);
            Assert.Equal([new BikeCarHub.Domain.ArticleFaq("How big is the engine?", "743cc.")], doc.Faqs);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ImageSize_reads_a_png_header()
    {
        byte[] png = [0x89, (byte)'P', (byte)'N', (byte)'G', 13, 10, 26, 10, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R', 0, 0, 1, 94, 0, 0, 0, 230];
        Assert.Equal((350, 230), ImageSize.Read(png));
    }

    [Fact]
    public void Shipped_article_list_and_himalayan_750_docx_parse()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BCHRazorPgaesClaude.csproj"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var entries = FileArticles.ReadManifest(Path.Combine(dir!.FullName, "Articles", FileArticles.ManifestName));
        var entry = Assert.Single(entries, e => e.Slug == "royal-enfield-himalayan-750");
        Assert.Equal(BikeCarHub.Domain.ArticleKind.Blog, FileArticles.KindOf(entry.Section));

        var doc = RichDocxReader.Read(Path.Combine(dir.FullName, "Articles", entry.Docx!), _ => null, "/images/default.webp")!;
        Assert.StartsWith("Royal Enfield Himalayan 750", doc.Title);
        Assert.Equal(7, doc.Faqs.Count);
        Assert.Contains("class=\"article-table\"", doc.BodyHtml);
        // The template's highlights survive: coloured stat boxes and the highlighted Himalayan 750 column.
        Assert.Contains("<li class=\"fill-accent\"><strong>55 PS</strong>", doc.BodyHtml);
        Assert.Contains("<th scope=\"col\" class=\"fill-accent center\"><strong>Himalayan 750</strong></th>", doc.BodyHtml);
        Assert.Contains("<td class=\"fill-peach center\">743 cc twin</td>", doc.BodyHtml);
        Assert.Equal("orange", doc.Theme);
    }

    [Fact]
    public void Shipped_hyundai_bayon_docx_parses_with_the_blue_theme()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BCHRazorPgaesClaude.csproj"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var entries = FileArticles.ReadManifest(Path.Combine(dir!.FullName, "Articles", FileArticles.ManifestName));
        var entry = Assert.Single(entries, e => e.Slug == "hyundai-bayon");
        Assert.False(entry.TwoWheeler);
        Assert.False(entry.MorePhotos);

        var doc = RichDocxReader.Read(Path.Combine(dir.FullName, "Articles", entry.Docx!), _ => null, "/images/default.webp")!;
        Assert.StartsWith("Hyundai Bayon", doc.Title);
        Assert.Equal("BIKECARHUB  ·  NEW CARS  ·  HYUNDAI", doc.Kicker);
        Assert.Equal("blue", doc.Theme);
        Assert.Equal(7, doc.Faqs.Count);
        Assert.Contains("<li class=\"fill-dark\"><strong>115 PS</strong>", doc.BodyHtml);
        Assert.Contains("<li class=\"fill-accent\"><strong>144 Nm</strong>", doc.BodyHtml);
        Assert.Contains("<figure class=\"pull-quote\">", doc.BodyHtml);
        Assert.Contains("<aside class=\"callout\">", doc.BodyHtml);
        // Variant names sit in blue row headers, as in the Word table.
        Assert.Matches("<th scope=\"row\" class=\"fill-accent[^\"]*\">(<strong>)?HX 2", doc.BodyHtml);
    }
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
