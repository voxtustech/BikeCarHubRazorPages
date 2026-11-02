using System.Text;

namespace BikeCarHub.Services;

/// <summary>
/// Writes the SQL that stores today's computed slugs in the Slug columns added by migration 001,
/// so every current URL stays exactly as it is once the site reads stored slugs.
/// </summary>
public static class SlugFillScript
{
    public static string Build(Catalog catalog, DateTime generatedUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/*");
        sb.AppendLine($"  007 - Fill Slug columns with the slugs the site uses today. Generated {generatedUtc:yyyy-MM-dd HH:mm} UTC.");
        sb.AppendLine("  Review before running. Run 001_add_slug_columns.sql first. Safe to run more than once:");
        sb.AppendLine("  a row that already has a slug is never changed. Inactive models are left empty.");
        sb.AppendLine("*/");
        sb.AppendLine("SET XACT_ABORT ON;");
        sb.AppendLine("BEGIN TRANSACTION;");
        sb.AppendLine();
        foreach (var brand in catalog.Brands)
            Line(sb, "TWBrands", "TWBrandId", brand.Id, brand.Slug);
        sb.AppendLine();
        foreach (var bike in catalog.Bikes)
            Line(sb, "TwoWheelers", "TwoWheelerId", bike.Id, bike.Slug);
        sb.AppendLine();
        foreach (var variant in catalog.Bikes.SelectMany(b => b.Variants))
            Line(sb, "TWVarients", "TWVarientId", variant.Id, variant.Slug);
        sb.AppendLine();
        sb.AppendLine("COMMIT TRANSACTION;");
        return sb.ToString();
    }

    private static void Line(StringBuilder sb, string table, string idColumn, int id, string slug) =>
        sb.Append("UPDATE ").Append(table).Append(" SET Slug = '").Append(slug.Replace("'", "''"))
          .Append("' WHERE ").Append(idColumn).Append(" = ").Append(id).AppendLine(" AND Slug IS NULL;");
}
