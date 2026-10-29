using System.Collections.Concurrent;
using BikeCarHub.Domain;

namespace BikeCarHub.Services;

public sealed record ElectricModel(Bike Bike, EvFacts Facts);

/// <summary>Every electric model whose battery size and range are known, so its running cost can be worked out.</summary>
public interface IElectricCatalog
{
    Task<IReadOnlyList<ElectricModel>> GetAsync();
    Task<ElectricModel?> FindAsync(Bike bike);
}

public sealed class ElectricCatalog(ICatalogService catalogService, ISpecService specService, ICacheStore cache) : IElectricCatalog
{
    public Task<IReadOnlyList<ElectricModel>> GetAsync() => cache.GetOrCreateAsync("electric", TimeSpan.FromMinutes(30), LoadAsync);

    /// <summary>Reads only this model's specs (cached per variant), so a single page never waits for the whole list.</summary>
    public async Task<ElectricModel?> FindAsync(Bike bike)
    {
        if (!bike.IsEv || bike.Variants.Count == 0) return null;
        var facts = EvFacts.From(await specService.GetSectionsAsync(bike.Variants[0]));
        return facts.CanCost ? new ElectricModel(bike, facts) : null;
    }

    private async Task<IReadOnlyList<ElectricModel>> LoadAsync()
    {
        var catalog = await catalogService.GetAsync();
        var found = new ConcurrentBag<ElectricModel>();
        // About a hundred models, each needing its own spec query; a few at a time keeps the first load short.
        await Parallel.ForEachAsync(catalog.Bikes.Where(b => b.IsEv && b.Variants.Count > 0), new ParallelOptions { MaxDegreeOfParallelism = 6 },
            async (bike, _) =>
            {
                var facts = EvFacts.From(await specService.GetSectionsAsync(bike.Variants[0]));
                if (facts.CanCost) found.Add(new ElectricModel(bike, facts));
            });
        return found
            .OrderBy(m => m.Bike.Brand.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Bike.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
