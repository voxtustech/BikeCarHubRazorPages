using System.Security.Claims;
using BikeCarHub.Data;
using BikeCarHub.Domain;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Wishlist;

public enum WishlistMode { Browser, Account, Shared }

public sealed record WishlistItem(Bike Bike, DateTime? Added, EvFacts? Ev);

public sealed record WishlistView(IReadOnlyList<WishlistItem> Items, WishlistMode Mode);

/// <summary>
/// Saved bikes live in the visitor's browser. When database writes are allowed, a signed-in visitor's list is
/// also kept in the Wishlist table so it follows them to other devices.
/// </summary>
public class IndexModel(ICatalogService catalogService, IElectricCatalog electricCatalog, IWishlistRepository repo, IWriteGate gate) : BasePageModel
{
    public const int MaxItems = 50;

    public WishlistMode Mode { get; private set; }
    public IReadOnlyList<WishlistItem> Items { get; private set; } = [];
    public bool AccountsCanSave => gate.Allowed;

    /// <summary>The account whose list is kept in the database, or null when the browser copy is the only one.</summary>
    public static int? SyncUserId(ClaimsPrincipal user, IWriteGate gate) =>
        gate.Allowed && user.Identity?.IsAuthenticated == true && int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public async Task OnGetAsync(string? bikes)
    {
        var catalog = await catalogService.GetAsync();
        if (bikes is not null)
        {
            Mode = WishlistMode.Shared;
            Items = await ToItemsAsync(FromSlugs(catalog, bikes).Select(b => (b, (DateTime?)null)));
        }
        else if (SyncUserId(User, gate) is { } userId)
        {
            Mode = WishlistMode.Account;
            Items = await ToItemsAsync(await FromAccountAsync(catalog, userId));
        }
        else
        {
            Mode = WishlistMode.Browser;
        }

        Meta.Title = Mode == WishlistMode.Shared ? "A Shared Bike Shortlist | BikeCarHub" : "Your Wishlist | BikeCarHub";
        Meta.Description = "Bikes and scooters saved to a BikeCarHub wishlist, with prices, key specifications and quick links to compare them.";
        Meta.CanonicalPath = "/wishlist";
        Meta.NoIndex = true;
        Meta.Crumbs(("Wishlist", "/wishlist"));
    }

    /// <summary>The list as HTML, for the browser copy that only the page's script can read.</summary>
    public async Task<IActionResult> OnGetItemsAsync(string? bikes)
    {
        var catalog = await catalogService.GetAsync();
        var items = await ToItemsAsync(FromSlugs(catalog, bikes).Select(b => (b, (DateTime?)null)));
        return Partial("_WishlistItems", new WishlistView(items, WishlistMode.Browser));
    }

    /// <summary>Adds the browser's bikes to the account, then returns the account's full list.</summary>
    public async Task<IActionResult> OnPostMergeAsync(string? bikes)
    {
        if (SyncUserId(User, gate) is not { } userId) return BadRequest();
        var catalog = await catalogService.GetAsync();
        await repo.AddAsync(userId, User.Identity?.Name, FromSlugs(catalog, bikes).Select(b => b.Id).ToList());
        var saved = await FromAccountAsync(catalog, userId);
        return new JsonResult(saved.Select(s => new { slug = s.Bike.FullSlug, added = new DateTimeOffset(DateTime.SpecifyKind(s.Added!.Value, DateTimeKind.Utc)).ToUnixTimeMilliseconds() }));
    }

    public async Task<IActionResult> OnPostToggleAsync(string? bike, bool saved)
    {
        if (SyncUserId(User, gate) is not { } userId) return BadRequest();
        var catalog = await catalogService.GetAsync();
        if (bike is null || !catalog.BikeByFullSlug.TryGetValue(bike, out var found)) return NotFound();
        if (saved) await repo.AddAsync(userId, User.Identity?.Name, [found.Id]);
        else await repo.RemoveAsync(userId, found.Id);
        return new JsonResult(new { ok = true });
    }

    private static IReadOnlyList<Bike> FromSlugs(Catalog catalog, string? slugs) =>
        (slugs ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .Select(s => catalog.BikeByFullSlug.GetValueOrDefault(s))
            .OfType<Bike>()
            .Take(MaxItems)
            .ToList();

    /// <summary>Saved rows for models that are still listed; removed models drop out quietly.</summary>
    private async Task<IReadOnlyList<(Bike Bike, DateTime? Added)>> FromAccountAsync(Catalog catalog, int userId)
    {
        var byId = catalog.Bikes.ToDictionary(b => b.Id);
        return (await repo.GetAsync(userId))
            .Where(r => byId.ContainsKey(r.TwoWheelerID))
            .Select(r => (byId[r.TwoWheelerID], (DateTime?)r.CreatedAt))
            .Take(MaxItems)
            .ToList();
    }

    private async Task<IReadOnlyList<WishlistItem>> ToItemsAsync(IEnumerable<(Bike Bike, DateTime? Added)> saved)
    {
        var items = new List<WishlistItem>();
        foreach (var (bike, added) in saved)
            items.Add(new WishlistItem(bike, added, (await electricCatalog.FindAsync(bike))?.Facts));
        return items;
    }
}
