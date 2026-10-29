using BikeCarHub.Domain;
using BikeCarHub.Services;
using BikeCarHub.Web;

namespace BikeCarHub.Pages.Electric;

public class IndexModel(ICatalogService catalogService, IElectricCatalog electricCatalog) : BasePageModel
{
    public IReadOnlyList<IGrouping<Brand, ElectricModel>> ByBrand { get; private set; } = [];
    public int ModelCount { get; private set; }
    public int ElectricCount { get; private set; }

    public async Task OnGetAsync()
    {
        var catalog = await catalogService.GetAsync();
        ElectricCount = catalog.Bikes.Count(b => b.IsEv);
        var models = await electricCatalog.GetAsync();
        ModelCount = models.Count;
        ByBrand = models.GroupBy(m => m.Bike.Brand).ToList();

        Meta.Title = "Electric Bikes and Scooters – Range, Charging and Running Cost | BikeCarHub";
        Meta.Description = "Compare the claimed range, charging time and monthly running cost of electric two-wheelers in India.";
        Meta.CanonicalPath = "/electric-bikes";
        Meta.NoIndex = ElectricSection.IsPilot;
        Meta.Crumbs(("Electric bikes", "/electric-bikes"));
    }
}
