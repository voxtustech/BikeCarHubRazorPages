using BikeCarHub.Domain;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Upcoming;

public class DetailModel(IContentService contentService, IImageUrlProvider images) : BasePageModel
{
    public UpcomingBike Bike { get; private set; } = null!;
    public LaunchStatus Status { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var all = (await contentService.GetAsync()).Upcoming;
        var bike = all.FirstOrDefault(u => u.Slug == slug);
        if (bike is null)
        {
            var normalized = Slugger.Slugify(slug);
            var match = all.FirstOrDefault(u => u.Slug == normalized);
            return match is not null ? Moved(match.Url) : NotFound();
        }

        Bike = bike;
        Status = bike.StatusOn(DateTime.Today);

        Meta.Title = Status == LaunchStatus.Upcoming
            ? $"{bike.Name} – Expected Launch and Details | BikeCarHub"
            : $"{bike.Name} – Launch Details | BikeCarHub";
        Meta.Description = $"{bike.Name}: expected launch {bike.ExpectedText ?? "date not available"} and what is known about it so far.";
        Meta.CanonicalPath = bike.Url;
        Meta.NoIndex = bike.BodyTextLength < 200;
        if (bike.ImagePath is not null) Meta.Image = Meta.PreloadImage = images.Resolve(bike.ImagePath);
        Meta.Crumbs(("Upcoming bikes", "/upcoming-bikes"), (bike.Name, bike.Url));
        return Page();
    }
}
