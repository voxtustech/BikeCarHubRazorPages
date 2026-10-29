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
