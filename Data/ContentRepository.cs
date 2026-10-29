using Dapper;

namespace BikeCarHub.Data;

public sealed class BlogRow
{
    public int BlogId { get; set; }
    public string? BlogHeading { get; set; }
    public string? BlogSummary { get; set; }
    public string? BlogDetail { get; set; }
    public string? URL { get; set; }
    public DateTime? Date { get; set; }
    public string? ImageURL { get; set; }
    public bool? IsTwoWheeler { get; set; }
}

public sealed class NewsRow
{
    public int TWLatestNewsId { get; set; }
    public string? NewsHeading { get; set; }
    public string? NewsSummary { get; set; }
    public string? NewsDetail { get; set; }
    /// <summary>Holds the page slug in the existing data, not an image.</summary>
    public string? ImageURL { get; set; }
    public DateTime? Date { get; set; }
    public string? ImageFolderURL { get; set; }
    public bool? IsTwoWheeler { get; set; }
}

public sealed class UpcomingRow
{
    public int UpcomingBikeId { get; set; }
    public string? UpcomingBikeName { get; set; }
    public string? UpcomingBikeDetails { get; set; }
    public string? ExpectedLaunchDate { get; set; }
    /// <summary>Holds the page slug in the existing data, not an image.</summary>
    public string? ImageURL { get; set; }
    public string? BrandName { get; set; }
    public DateTime? FilterLaunchDate { get; set; }
    public DateTime? CreatedDateTime { get; set; }
}

public sealed class ValueForMoneyRow
{
    public int VFMId { get; set; }
    public string? VFMHeading { get; set; }
    public string? URL { get; set; }
    public DateTime? Date { get; set; }
    public bool? IsTwoWheeler { get; set; }
    public string? ImageURL { get; set; }
}

public sealed class HeroSlideRow
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? ImageURL { get; set; }
    public string? Badge { get; set; }
    public string? Featured { get; set; }
    public string? Price { get; set; }
}

public interface IContentRepository
{
    Task<IReadOnlyList<BlogRow>> GetBlogsAsync();
    Task<IReadOnlyList<NewsRow>> GetNewsAsync();
    Task<IReadOnlyList<UpcomingRow>> GetUpcomingAsync();
    Task<IReadOnlyList<ValueForMoneyRow>> GetValueForMoneyAsync();
    Task<IReadOnlyList<HeroSlideRow>> GetHeroSlidesAsync();
    Task<IReadOnlyList<string>> GetCompareTopicsAsync();
}

public sealed class SqlContentRepository(IDbConnectionFactory db) : IContentRepository
{
    public async Task<IReadOnlyList<BlogRow>> GetBlogsAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<BlogRow>(
            "SELECT BlogId, BlogHeading, BlogSummary, BlogDetail, URL, Date, ImageURL, IsTwoWheeler FROM Blogs")).AsList();
    }

    public async Task<IReadOnlyList<NewsRow>> GetNewsAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<NewsRow>(
            "SELECT TWLatestNewsId, NewsHeading, NewsSummary, NewsDetail, ImageURL, Date, ImageFolderURL, IsTwoWheeler FROM LatestNews")).AsList();
    }

    public async Task<IReadOnlyList<UpcomingRow>> GetUpcomingAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<UpcomingRow>(
            "SELECT UpcomingBikeId, UpcomingBikeName, UpcomingBikeDetails, ExpectedLaunchDate, ImageURL, BrandName, FilterLaunchDate, CreatedDateTime FROM TWUpcomingBikes")).AsList();
    }

    public async Task<IReadOnlyList<ValueForMoneyRow>> GetValueForMoneyAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<ValueForMoneyRow>(
            "SELECT VFMId, VFMHeading, URL, Date, IsTwoWheeler, ImageURL FROM ValueForMoney")).AsList();
    }

    public async Task<IReadOnlyList<HeroSlideRow>> GetHeroSlidesAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<HeroSlideRow>(
            "SELECT Title, Subtitle, ImageURL, Badge, Featured, Price FROM HeroSlides WHERE IsActive = 1 ORDER BY DisplayOrder")).AsList();
    }

    public async Task<IReadOnlyList<string>> GetCompareTopicsAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<string>("SELECT Topic FROM CompareItems ORDER BY Id")).AsList();
    }
}
