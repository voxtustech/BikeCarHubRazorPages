using BikeCarHub.Domain;
using BikeCarHub.Services;
using BikeCarHub.Web;

namespace BikeCarHub.Pages.Upcoming;

public class IndexModel(IContentService contentService) : BasePageModel
{
    public IReadOnlyList<UpcomingBike> Upcoming { get; private set; } = [];
    public IReadOnlyList<UpcomingBike> Released { get; private set; } = [];
    public IReadOnlyList<UpcomingBike> DatePassed { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var all = (await contentService.GetAsync()).Upcoming;
        // Status comes from the dates, so an old "upcoming" entry is never presented as upcoming.
        var byStatus = all.ToLookup(u => u.StatusOn(DateTime.Today));
        Upcoming = byStatus[LaunchStatus.Upcoming].OrderBy(u => u.ExpectedDate).ToList();
        Released = byStatus[LaunchStatus.Released].ToList();
        DatePassed = byStatus[LaunchStatus.DatePassed].ToList();

        Meta.Title = "Upcoming Bikes in India – Expected Launches | BikeCarHub";
        Meta.Description = "Bikes and scooters expected to launch in India, with expected launch dates and what is known so far.";
        Meta.CanonicalPath = "/upcoming-bikes";
        Meta.NoIndex = Request.Query.Count > 0;
        Meta.Crumbs(("Upcoming bikes", "/upcoming-bikes"));
    }
}
