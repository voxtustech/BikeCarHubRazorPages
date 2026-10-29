using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages;

/// <summary>Home-page search. Never renders: it sends the visitor to the matching brand, model or filtered list.</summary>
public class SearchModel(ICatalogService catalogService) : BasePageModel
{
    public async Task<IActionResult> OnGetAsync(string? q, string? brand, string? model, string? budget)
    {
        var catalog = await catalogService.GetAsync();
        if (!string.IsNullOrWhiteSpace(q))
        {
            // Header search box: an exact model name goes straight to that model.
            return catalog.FindBikeByName(q) is { } exact
                ? Redirect(exact.Url)
                : Redirect("/new-bikes?q=" + Uri.EscapeDataString(q.Trim()));
        }
        if (string.IsNullOrWhiteSpace(brand) || !catalog.BrandBySlug.TryGetValue(brand, out var found))
            return Redirect("/new-bikes");

        if (!string.IsNullOrWhiteSpace(model) && catalog.FindBike(found.Slug, model) is { } bike)
            return Redirect(bike.Url);

        var parts = budget?.Split('-');
        if (parts is { Length: 2 } && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
        {
            var query = new List<string>();
            if (min > 0) query.Add($"min={min}");
            if (max > 0) query.Add($"max={max}");
            return Redirect(query.Count > 0 ? $"{found.Url}?{string.Join('&', query)}" : found.Url);
        }
        return Redirect(found.Url);
    }
}
