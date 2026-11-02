using BikeCarHub.Data;
using BikeCarHub.Domain;
using BikeCarHub.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BikeCarHub.Tests;

public class SlugAllocatorTests
{
    [Fact]
    public void Without_stored_slugs_it_behaves_like_before()
    {
        var slugs = new SlugAllocator([null, null, null]);
        Assert.Equal("std", slugs.Allocate(null, "std", 1));
        Assert.Equal("std-2", slugs.Allocate(null, "std", 2));
        Assert.Equal("3", slugs.Allocate(null, "", 3));
    }

    [Fact]
    public void A_stored_slug_wins_and_computed_slugs_never_take_it()
    {
        // Row 1 would compute "classic" but row 2 has stored it, so row 1 moves aside.
        var slugs = new SlugAllocator([null, "classic"]);
        Assert.Equal("classic-1", slugs.Allocate(null, "classic", 1));
        Assert.Equal("classic", slugs.Allocate("classic", "anything", 2));
    }

    [Fact]
    public void A_repeated_stored_slug_goes_to_its_first_owner()
    {
        var slugs = new SlugAllocator(["dup", "dup"]);
        Assert.Equal("dup", slugs.Allocate("dup", "first", 1));
        Assert.Equal("second", slugs.Allocate("dup", "second", 2));
    }

    [Fact]
    public void Stored_slugs_are_normalised() =>
        Assert.Equal("pulsar-150", new SlugAllocator(["Pulsar 150 "]).Allocate("Pulsar 150 ", "x", 1));
}

public class CatalogSlugTests
{
    private sealed class FakeRepo : ICatalogRepository
    {
        public List<BrandRow> Brands { get; } = [];
        public List<BikeRow> Bikes { get; } = [];
        public List<VariantRow> Variants { get; } = [];
        public List<SlugHistoryRow> History { get; } = [];
        public Task<IReadOnlyList<BrandRow>> GetBrandsAsync() => Task.FromResult<IReadOnlyList<BrandRow>>(Brands);
        public Task<IReadOnlyList<BikeRow>> GetBikesAsync() => Task.FromResult<IReadOnlyList<BikeRow>>(Bikes);
        public Task<IReadOnlyList<VariantRow>> GetVariantsAsync() => Task.FromResult<IReadOnlyList<VariantRow>>(Variants);
        public Task<IReadOnlyList<BikeFigureRow>> GetDisplacementsAsync() => Task.FromResult<IReadOnlyList<BikeFigureRow>>([]);
        public Task<IReadOnlyList<BikeFigureRow>> GetMileagesAsync() => Task.FromResult<IReadOnlyList<BikeFigureRow>>([]);
        public Task<IReadOnlyList<IDictionary<string, object?>?>> GetSpecRowsAsync(IReadOnlyList<string> tables, int variantId) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SlugHistoryRow>> GetSlugHistoryAsync() => Task.FromResult<IReadOnlyList<SlugHistoryRow>>(History);
    }

    private sealed class NoCache : ICacheStore
    {
        public Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory) where T : class => factory();
    }

    private static FakeRepo Sample()
    {
        var repo = new FakeRepo();
        repo.Brands.Add(new BrandRow { TWBrandId = 1, BrandName = "Royal Enfield" });
        repo.Bikes.Add(new BikeRow { TwoWheelerId = 10, TwoWheelerName = "Royal Enfield Continental GT 650", TwoWBrandId = 1, TWImage = "/images/x.webp", IsActive = true });
        repo.Variants.Add(new VariantRow { TWVarientId = 100, Varients = "Royal Enfield Continental GT 650 Chrome", TwoWheelerId = 10 });
        return repo;
    }

    private static Task<Catalog> Load(FakeRepo repo) => new CatalogService(repo, new NoCache(), null!).GetAsync();

    [Fact]
    public async Task Computed_slugs_are_unchanged_when_nothing_is_stored()
    {
        var bike = (await Load(Sample())).Bikes.Single();
        Assert.Equal("/new-bikes/royal-enfield/continental-gt-650", bike.Url);
        Assert.Equal("/new-bikes/royal-enfield/continental-gt-650/chrome", bike.Variants.Single().Url);
    }

    [Fact]
    public async Task Stored_slugs_are_used_for_brand_model_and_variant()
    {
        var repo = Sample();
        repo.Brands[0].Slug = "re";
        repo.Bikes[0].Slug = "gt-650";
        repo.Variants[0].Slug = "chrome-edition";
        var bike = (await Load(repo)).Bikes.Single();
        Assert.Equal("/new-bikes/re/gt-650/chrome-edition", bike.Variants.Single().Url);
    }

    [Fact]
    public async Task Slug_history_redirects_old_paths_and_the_pages_below_them()
    {
        var repo = Sample();
        repo.Bikes[0].Slug = "gt-650";
        repo.History.Add(new SlugHistoryRow { EntityType = "model", EntityId = 10, OldPath = "/new-bikes/royal-enfield/continental-gt-650/" });
        repo.History.Add(new SlugHistoryRow { EntityType = "model", EntityId = 999, OldPath = "/new-bikes/royal-enfield/unknown" });
        var catalog = await Load(repo);

        Assert.Equal("/new-bikes/royal-enfield/gt-650", catalog.FindRedirect("/new-bikes/royal-enfield/continental-gt-650"));
        Assert.Equal("/new-bikes/royal-enfield/gt-650/chrome", catalog.FindRedirect("/New-Bikes/royal-enfield/continental-gt-650/chrome"));
        Assert.Null(catalog.FindRedirect("/new-bikes/royal-enfield/unknown"));
        Assert.Null(catalog.FindRedirect("/new-bikes/honda/shine"));
    }

    [Fact]
    public async Task A_history_row_pointing_at_the_current_address_is_ignored()
    {
        var repo = Sample();
        repo.History.Add(new SlugHistoryRow { EntityType = "model", EntityId = 10, OldPath = "/new-bikes/royal-enfield/continental-gt-650" });
        Assert.Empty((await Load(repo)).Redirects);
    }

    [Fact]
    public async Task Fill_script_stores_every_current_slug_without_overwriting()
    {
        var script = SlugFillScript.Build(await Load(Sample()), new DateTime(2026, 11, 2));
        Assert.Contains("UPDATE TWBrands SET Slug = 'royal-enfield' WHERE TWBrandId = 1 AND Slug IS NULL;", script);
        Assert.Contains("UPDATE TwoWheelers SET Slug = 'continental-gt-650' WHERE TwoWheelerId = 10 AND Slug IS NULL;", script);
        Assert.Contains("UPDATE TWVarients SET Slug = 'chrome' WHERE TWVarientId = 100 AND Slug IS NULL;", script);
    }
}

public class SignInThrottleTests
{
    private sealed class ManualClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void Locks_after_five_failures_and_unlocks_after_the_window()
    {
        var clock = new ManualClock();
        var throttle = new SignInThrottle(clock);
        for (var i = 0; i < SignInThrottle.MaxFailures - 1; i++) throttle.RecordFailure("a@b.com");
        Assert.False(throttle.IsLocked("a@b.com"));

        throttle.RecordFailure(" A@B.com ");
        Assert.True(throttle.IsLocked("a@b.com"));
        Assert.False(throttle.IsLocked("other@b.com"));

        clock.Now += SignInThrottle.Window;
        Assert.False(throttle.IsLocked("a@b.com"));
    }

    [Fact]
    public void A_successful_sign_in_clears_the_count()
    {
        var throttle = new SignInThrottle(new ManualClock());
        for (var i = 0; i < SignInThrottle.MaxFailures - 1; i++) throttle.RecordFailure("a@b.com");
        throttle.Reset("a@b.com");
        throttle.RecordFailure("a@b.com");
        Assert.False(throttle.IsLocked("a@b.com"));
    }

    [Fact]
    public void Identity_lockout_in_the_database_is_honoured()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.True(new AppUser { LockoutEnabled = true, LockoutEnd = now.AddMinutes(5) }.IsLockedOut(now));
        Assert.False(new AppUser { LockoutEnabled = true, LockoutEnd = now.AddMinutes(-5) }.IsLockedOut(now));
        Assert.False(new AppUser { LockoutEnabled = false, LockoutEnd = now.AddMinutes(5) }.IsLockedOut(now));
    }
}

public class ImageServingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bch-img-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private ImageUrlProvider Provider() => new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Images:LocalRoot"] = _root,
            ["Images:RemoteBaseUrl"] = "https://old.example.com"
        }).Build(), null!);

    [Theory]
    [InlineData("twowheeler/a.webp", true)]
    [InlineData("blogs/x/photo.JPG", true)]
    [InlineData("blogs/x/Article.docx", false)]
    [InlineData("blogs/x/notes.txt", false)]
    [InlineData("", false)]
    public void Only_image_files_are_served(string path, bool expected) =>
        Assert.Equal(expected, ImageUrlProvider.IsImageFile(path));

    [Fact]
    public void A_downloaded_copy_is_served_locally_instead_of_the_old_host()
    {
        const string path = "/images/twowheelerimages/bajaj/bajaj pulsar ns200.webp";
        Assert.StartsWith("https://old.example.com/", Provider().Resolve(path));

        var copy = Path.Combine(_root, ImageUrlProvider.MirrorFolder, "twowheelerimages", "bajaj", "bajaj pulsar ns200.webp");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.WriteAllBytes(copy, [0]);

        var provider = Provider();
        Assert.Equal("/images/twowheelerimages/bajaj/bajaj%20pulsar%20ns200.webp", provider.Resolve(path));
        Assert.Equal(copy, provider.MapToLocalFile(path));
    }

    [Fact]
    public void The_mirror_never_reaches_outside_the_image_root() =>
        Assert.Null(Provider().MapToLocalFile("/images/../../etc/passwd.png"));
}
