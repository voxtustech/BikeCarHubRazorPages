using System.Globalization;
using System.Text;

namespace BikeCarHub.Services;

public static class Slugger
{
    /// <summary>Lowercase, hyphenated, ASCII-only slug. "Bajaj Pulsar NS200" becomes "bajaj-pulsar-ns200".</summary>
    public static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var normalized = value.Replace("&", " and ").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var pendingHyphen = false;
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            var lower = char.ToLowerInvariant(c);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (pendingHyphen && sb.Length > 0) sb.Append('-');
                pendingHyphen = false;
                sb.Append(lower);
            }
            else
            {
                pendingHyphen = true;
            }
        }
        return sb.ToString();
    }

    /// <summary>Removes a leading prefix ("Bajaj" from "Bajaj Pulsar 150") when the remainder is not empty.</summary>
    public static string StripPrefix(string name, string prefix)
    {
        name = name.Trim();
        prefix = prefix.Trim();
        if (prefix.Length > 0 && name.Length > prefix.Length && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && !char.IsLetterOrDigit(name[prefix.Length]))
        {
            var rest = name[prefix.Length..].Trim(' ', '-', '(', ')');
            if (rest.Length > 0) return rest;
        }
        return name;
    }

    /// <summary>Removes the leading words a model name shares with its brand: "Ola Roadster" under "Ola Electric" becomes "Roadster".</summary>
    public static string StripBrand(string name, string brand)
    {
        var words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var brandWords = brand.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var shared = 0;
        while (shared < words.Length && shared < brandWords.Length
               && string.Equals(words[shared], brandWords[shared], StringComparison.OrdinalIgnoreCase)) shared++;
        return shared > 0 && shared < words.Length ? string.Join(' ', words[shared..]) : name.Trim();
    }

    /// <summary>
    /// Finds the item an old or mistyped slug was meant for. Tries, in order: the exact slug, the slug rewritten
    /// with <see cref="Slugify"/> (capitals, ₹, colons, en dashes), the same letters without hyphens
    /// ("mahindraBE-formulaE-edition", "18,000" vs "18000"), and finally the one item whose hyphen-free slug starts
    /// with, or is the start of, the wanted one ("marutisuzukibrezza" vs "maruti-suzuki-brezza-the-value-for-money-variant").
    /// Returns null rather than guessing when more than one item fits equally well.
    /// </summary>
    public static T? Match<T>(IEnumerable<T> items, string? wanted, Func<T, string> slugOf) where T : class
    {
        if (string.IsNullOrWhiteSpace(wanted)) return null;
        var list = items as IReadOnlyList<T> ?? items.ToList();
        var exact = list.FirstOrDefault(i => slugOf(i) == wanted);
        if (exact is not null) return exact;

        var slug = Slugify(wanted);
        if (slug.Length == 0) return null;
        var normalized = list.FirstOrDefault(i => slugOf(i) == slug);
        if (normalized is not null) return normalized;

        var compact = slug.Replace("-", "");
        var sameLetters = list.Where(i => slugOf(i).Replace("-", "") == compact).Take(2).ToList();
        if (sameLetters.Count == 1) return sameLetters[0];
        if (compact.Length < 8) return null;

        var compacts = list.Select(i => (Item: i, Compact: slugOf(i).Replace("-", ""))).ToList();
        // The wanted slug is a shortened form: only one item may start with it.
        var longer = compacts.Where(x => x.Compact.StartsWith(compact, StringComparison.Ordinal)).Take(2).ToList();
        if (longer.Count == 1) return longer[0].Item;
        if (longer.Count > 1) return null;
        // The wanted slug has extra words: take the longest item it starts with.
        return compacts
            .Where(x => x.Compact.Length >= 8 && compact.StartsWith(x.Compact, StringComparison.Ordinal))
            .OrderByDescending(x => x.Compact.Length)
            .Select(x => x.Item)
            .FirstOrDefault();
    }

    /// <summary>Makes <paramref name="slug"/> unique within <paramref name="used"/> by appending the record id.</summary>
    public static string Unique(string slug, int id, HashSet<string> used)
    {
        if (slug.Length == 0) slug = id.ToString(CultureInfo.InvariantCulture);
        if (!used.Add(slug))
        {
            slug = $"{slug}-{id}";
            used.Add(slug);
        }
        return slug;
    }
}
