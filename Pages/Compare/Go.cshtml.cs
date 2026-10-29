using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Compare;

/// <summary>Turns the compare form's selections into the shareable /compare/a-vs-b address.</summary>
public class GoModel(ICatalogService catalogService) : BasePageModel
{
    public async Task<IActionResult> OnGetAsync(string? a, string? b, string? c)
    {
        var catalog = await catalogService.GetAsync();
        var bikes = new[] { a, b, c }
            .Where(s => !string.IsNullOrWhiteSpace(s) && catalog.BikeByFullSlug.ContainsKey(s))
            .Distinct()
            .Select(s => catalog.BikeByFullSlug[s!])
            .ToList();
        return bikes.Count switch
        {
            >= 2 => Redirect(IndexModel.UrlFor(bikes)),
            1 => Redirect($"/compare?a={bikes[0].FullSlug}"),
            _ => Redirect("/compare")
        };
    }
}
