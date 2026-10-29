using Dapper;

namespace BikeCarHub.Data;

public sealed class BrandRow
{
    public int TWBrandId { get; set; }
    public string? BrandName { get; set; }
    public bool? IsPetrol { get; set; }
    public bool? IsEV { get; set; }
    public string? BrandLogoURL { get; set; }
    public string? Discription { get; set; }
    public string? TitleTag { get; set; }
    public string? MetaDescription { get; set; }
    public DateTime? CreatedDateTime { get; set; }
}

public sealed class BikeRow
{
    public int TwoWheelerId { get; set; }
    public string? TwoWheelerName { get; set; }
    public string? Price { get; set; }
    public int? BasePrice { get; set; }
    public int? TopPrice { get; set; }
    public string? Type { get; set; }
    public int? TwoWBrandId { get; set; }
    public string? TWImage { get; set; }
    public string? LaunchDate { get; set; }
    public bool? IsEV { get; set; }
    public bool? IsActive { get; set; }
    public string? Discription { get; set; }
    public DateTime? CreatedDateTime { get; set; }
}

public sealed class VariantRow
{
    public int TWVarientId { get; set; }
    public string? Varients { get; set; }
    public int? TwoWheelerId { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public string? SEO_Title { get; set; }
    public string? SEO_Description { get; set; }
}

public sealed class BikeFigureRow
{
    public int TwoWheelerId { get; set; }
    public decimal? Value { get; set; }
}

public interface ICatalogRepository
{
    Task<IReadOnlyList<BrandRow>> GetBrandsAsync();
    Task<IReadOnlyList<BikeRow>> GetBikesAsync();
    Task<IReadOnlyList<VariantRow>> GetVariantsAsync();
    Task<IReadOnlyList<BikeFigureRow>> GetDisplacementsAsync();
    Task<IReadOnlyList<BikeFigureRow>> GetMileagesAsync();
    /// <summary>One result set per table in <paramref name="tables"/>, first row only, as column/value pairs.</summary>
    Task<IReadOnlyList<IDictionary<string, object?>?>> GetSpecRowsAsync(IReadOnlyList<string> tables, int variantId);
}

public sealed class SqlCatalogRepository(IDbConnectionFactory db) : ICatalogRepository
{
    // Spec tables are read with SELECT * so new columns appear without code changes.
    // Table names come only from this whitelist; the variant id is always a parameter.
    public static readonly IReadOnlyList<string> SpecTables =
    [
        "TWSpec", "TWMileageAndPerformances", "TWEngineAndTransmissions", "TWFeatures", "TWSafety",
        "TWDimensionsAndCapacities", "TWElectricals", "TWTyresAndBrakes", "TWMotorAndBatteries",
        "TWUnderpinnings", "TWChargings", "TWChassisAndSuspensions",
        "TWEVRanges", "TWEVPerformances", "TWEVMotorAndBatteries", "TWEVChargings", "TWEVEngineAndTransmissions",
        "TWEVFeatures", "TWEVAppFeatures", "TWEVSafety", "TWEVDimensionsAndCapacities", "TWEVElectricals",
        "TWEVTyresAndBrakes", "TWEVUnderpinnings", "TWEVChassisAndSuspensions"
    ];

    private static readonly Dictionary<string, string> SpecSql =
        SpecTables.ToDictionary(t => t, t => $"SELECT TOP 1 * FROM [{t}] WHERE TWVarientId = @id ORDER BY 1;");

    public async Task<IReadOnlyList<BrandRow>> GetBrandsAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<BrandRow>(
            "SELECT TWBrandId, BrandName, IsPetrol, IsEV, BrandLogoURL, Discription, TitleTag, MetaDescription, CreatedDateTime FROM TWBrands WHERE IsTWBrand = 1")).AsList();
    }

    public async Task<IReadOnlyList<BikeRow>> GetBikesAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<BikeRow>(
            "SELECT TwoWheelerId, TwoWheelerName, Price, BasePrice, TopPrice, Type, TwoWBrandId, TWImage, LaunchDate, IsEV, IsActive, Discription, CreatedDateTime FROM TwoWheelers")).AsList();
    }

    public async Task<IReadOnlyList<VariantRow>> GetVariantsAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<VariantRow>(
            "SELECT TWVarientId, Varients, TwoWheelerId, CreatedDateTime, SEO_Title, SEO_Description FROM TWVarients ORDER BY TWVarientId")).AsList();
    }

    public async Task<IReadOnlyList<BikeFigureRow>> GetDisplacementsAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<BikeFigureRow>(
            "SELECT TwoWheelerId, MAX(Displacement) AS Value FROM TWEngineAndTransmissions WHERE TwoWheelerId IS NOT NULL GROUP BY TwoWheelerId")).AsList();
    }

    public async Task<IReadOnlyList<BikeFigureRow>> GetMileagesAsync()
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<BikeFigureRow>(
            "SELECT TwoWheelerId, MAX(Milage) AS Value FROM TWSpec WHERE TwoWheelerId IS NOT NULL GROUP BY TwoWheelerId")).AsList();
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>?>> GetSpecRowsAsync(IReadOnlyList<string> tables, int variantId)
    {
        var sql = string.Concat(tables.Select(t => SpecSql[t]));
        await using var conn = await db.OpenAsync();
        using var multi = await conn.QueryMultipleAsync(sql, new { id = variantId });
        var result = new List<IDictionary<string, object?>?>(tables.Count);
        foreach (var _ in tables)
        {
            var row = (await multi.ReadAsync()).FirstOrDefault();
            result.Add(row is null ? null : (IDictionary<string, object?>)row);
        }
        return result;
    }
}
