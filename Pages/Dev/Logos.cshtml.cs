using BikeCarHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BikeCarHub.Pages.Dev;

// Temporary side-by-side view of the candidate SVG logos. Development only; delete once a logo is chosen.
public class LogosModel(IImageUrlProvider images, IWebHostEnvironment env) : PageModel
{
    public IReadOnlyList<(string Name, string Url)> Logos { get; private set; } = [];

    public IActionResult OnGet()
    {
        if (!env.IsDevelopment()) return NotFound();
        var folder = images.MapToLocalDirectory("/images/bchlogo/x.svg");
        if (folder is null) return NotFound();
        Logos = Directory.EnumerateFiles(folder, "*.svg")
            .Select(Path.GetFileName)
            .OrderBy(n => int.TryParse(Path.GetFileNameWithoutExtension(n), out var i) ? i : int.MaxValue)
            .ThenBy(n => n)
            .Select(n => (n!, images.Resolve("/images/bchlogo/" + n)))
            .ToList();
        return Page();
    }
}
