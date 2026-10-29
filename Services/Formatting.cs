using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace BikeCarHub.Services;

public static partial class Formatting
{
    public const string NotAvailable = "Not available";

    [GeneratedRegex(@"\d[\d,]*")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex WordBoundaryRegex();

    /// <summary>Reads rupee amounts out of text such as "Rs. 1,13,734 - 1,20,819".</summary>
    public static (int? Min, int? Max) ParsePriceRange(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, null);
        var values = new List<int>();
        foreach (Match m in NumberRegex().Matches(text))
            if (int.TryParse(m.Value.Replace(",", ""), NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v >= 1000)
                values.Add(v);
        return values.Count == 0 ? (null, null) : (values.Min(), values.Max());
    }

    /// <summary>Indian digit grouping: 113734 becomes "₹1,13,734".</summary>
    public static string Rupees(long amount)
    {
        var digits = Math.Abs(amount).ToString(CultureInfo.InvariantCulture);
        if (digits.Length > 3)
        {
            var head = digits[..^3];
            var sb = new StringBuilder();
            for (var i = 0; i < head.Length; i++)
            {
                if (i > 0 && (head.Length - i) % 2 == 0) sb.Append(',');
                sb.Append(head[i]);
            }
            digits = $"{sb},{digits[^3..]}";
        }
        return (amount < 0 ? "-₹" : "₹") + digits;
    }

    public static string PriceShort(int amount) => amount switch
    {
        >= 10_000_000 => $"₹{(amount / 10_000_000m).ToString("0.##", CultureInfo.InvariantCulture)} crore",
        >= 100_000 => $"₹{(amount / 100_000m).ToString("0.##", CultureInfo.InvariantCulture)} lakh",
        _ => Rupees(amount)
    };

    public static string PriceRange(int? min, int? max)
    {
        if (min is null && max is null) return "Price not available";
        var lo = min ?? max!.Value;
        var hi = max ?? min!.Value;
        return lo == hi ? PriceShort(lo) : $"{PriceShort(lo)} – {PriceShort(hi)}";
    }

    public static string Number(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static readonly Dictionary<string, string> LabelOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Milage"] = "Mileage",
        ["LowFuelIndicato"] = "Low fuel indicator",
        ["NavigationAssis"] = "Navigation assist",
        ["NumOfCylinders"] = "Number of cylinders",
        ["NumOfBattries"] = "Number of batteries",
        ["ZeroTo40Kmphsec"] = "0–40 km/h (seconds)",
        ["ZeroTo100Kmphsec"] = "0–100 km/h (seconds)",
        ["ChargingTimeZeroTo80Percent"] = "Charging time 0–80%",
        ["ChargingTimeZeroTo100Percent"] = "Charging time 0–100%",
        ["ABS"] = "ABS"
    };

    private static readonly HashSet<string> Acronyms = new(StringComparer.OrdinalIgnoreCase)
        { "ABS", "LED", "USB", "EV", "SMS", "DRL", "GPS", "OTA", "IP", "CBS", "TFT", "LCD" };

    /// <summary>Turns a column name such as "GroundClearance" into "Ground clearance".</summary>
    public static string Humanize(string column)
    {
        if (LabelOverrides.TryGetValue(column, out var known)) return known;
        var words = WordBoundaryRegex().Replace(column.Replace('_', ' '), " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            if (Acronyms.Contains(words[i])) words[i] = words[i].ToUpperInvariant();
            else if (i == 0) words[i] = char.ToUpperInvariant(words[i][0]) + words[i][1..].ToLowerInvariant();
            else words[i] = words[i].ToLowerInvariant();
        }
        return string.Join(' ', words);
    }

    /// <summary>Plain database text to encoded paragraphs. Returns the HTML and the visible text length.</summary>
    public static (string? Html, int Length) TextToHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, 0);
        var trimmed = text.Trim();
        if (trimmed.Equals("NULL", StringComparison.OrdinalIgnoreCase)) return (null, 0);
        var sb = new StringBuilder();
        foreach (var para in trimmed.Replace("\r\n", "\n").Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            sb.Append("<p>").Append(WebUtility.HtmlEncode(para)).Append("</p>");
        return (sb.ToString(), trimmed.Length);
    }

    public static string Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.ReplaceLineEndings(" ").Trim();
        if (t.Length <= max) return t;
        var cut = t.LastIndexOf(' ', max - 1);
        return t[..(cut > max / 2 ? cut : max - 1)].TrimEnd(' ', ',', '.', ';', ':') + "…";
    }

    private static readonly string[] LaunchFormats = ["dd MMM yyyy", "d MMM yyyy", "dd MMMM yyyy", "d MMMM yyyy", "MMM yyyy", "MMMM yyyy", "yyyy"];

    public static DateTime? ParseLaunchDate(string? text) =>
        DateTime.TryParseExact(text?.Trim(), LaunchFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
