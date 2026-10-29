namespace BikeCarHub.Domain;

public sealed class Brand
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; set; } = "";
    public string? LogoPath { get; init; }
    public bool HasPetrol { get; init; }
    public bool HasEv { get; init; }
    public string? Description { get; init; }
    public string? TitleTag { get; init; }
    public string? MetaDescription { get; init; }
    public DateTime? Created { get; init; }
    public List<Bike> Bikes { get; } = new();
    public string Url => $"/new-bikes/{Slug}";
}

public sealed class Bike
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string ShortName { get; set; } = "";
    public string Slug { get; set; } = "";
    public Brand Brand { get; init; } = null!;
    public string? Type { get; init; }
    public string? ImagePath { get; init; }
    public int? MinPrice { get; init; }
    public int? MaxPrice { get; init; }
    public string? LaunchText { get; init; }
    public DateTime? LaunchDate { get; init; }
    public bool IsEv { get; init; }
    public bool IsActive { get; init; }
    public string? Description { get; init; }
    public DateTime? Created { get; init; }
    public decimal? DisplacementCc { get; set; }
    public decimal? MileageKmpl { get; set; }
    public List<Variant> Variants { get; } = new();
    public string FullSlug => $"{Brand.Slug}-{Slug}";
    public string Url => $"/new-bikes/{Brand.Slug}/{Slug}";
    public bool IsScooter => string.Equals(Type, "Scooter", StringComparison.OrdinalIgnoreCase);
}

public sealed class Variant
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string ShortName { get; set; } = "";
    public string Slug { get; set; } = "";
    public Bike Bike { get; init; } = null!;
    public DateTime? Created { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public string Url => $"{Bike.Url}/{Slug}";
}

public sealed record SpecItem(string Label, string Value);
public sealed record SpecSection(string Title, IReadOnlyList<SpecItem> Items);

public enum ArticleKind { Blog, News, ValueForMoney }

public sealed class Article
{
    public int Id { get; init; }
    public ArticleKind Kind { get; init; }
    public string Title { get; init; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; init; }
    public string? ImagePath { get; init; }
    public DateTime? Date { get; init; }
    public bool IsTwoWheeler { get; init; }
    /// <summary>Already-encoded HTML built from the database text or the article's .docx file.</summary>
    public string? BodyHtml { get; set; }
    public int BodyTextLength { get; set; }
    public List<string> GalleryPaths { get; } = new();
    public string BasePath => Kind switch { ArticleKind.Blog => "/blog", ArticleKind.News => "/news", _ => "/value-for-money" };
    public string Url => $"{BasePath}/{Slug}";
    /// <summary>Thin or empty articles stay reachable but are kept out of the index and the sitemap.</summary>
    public bool IsIndexable => BodyTextLength >= 200;
}

public enum LaunchStatus { Upcoming, Released, DatePassed }

public sealed class UpcomingBike
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; set; } = "";
    public string? BrandName { get; init; }
    public string? ExpectedText { get; init; }
    public DateTime? ExpectedDate { get; init; }
    public DateTime? Created { get; init; }
    public string? ImagePath { get; set; }
    public string? BodyHtml { get; set; }
    public int BodyTextLength { get; set; }
    public Bike? ReleasedAs { get; set; }
    public string Url => $"/upcoming-bikes/{Slug}";

    public LaunchStatus StatusOn(DateTime today) =>
        ReleasedAs is not null ? LaunchStatus.Released
        : ExpectedDate is { } d && d.Date >= today.Date ? LaunchStatus.Upcoming
        : LaunchStatus.DatePassed;
}

public sealed record HeroSlide(string Title, string? Subtitle, string? ImagePath, string? Badge, string? Featured, string? Price);

public sealed class AppUser
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
}
