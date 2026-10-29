using BikeCarHub.Domain;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Articles;

public class ListModel(IContentService contentService) : BasePageModel
{
    public const int PageSize = 12;

    public string Heading { get; private set; } = "";
    public IReadOnlyList<Article> Articles { get; private set; } = [];
    public int PageNumber { get; private set; }
    public int TotalPages { get; private set; }

    /// <summary>The section is taken from the first URL segment: /news, /blog or /value-for-money.</summary>
    public static (ArticleKind Kind, string BasePath, string Plural) SectionOf(PathString path)
    {
        var first = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first switch
        {
            "blog" => (ArticleKind.Blog, "/blog", "Blogs"),
            "value-for-money" => (ArticleKind.ValueForMoney, "/value-for-money", "Value for money"),
            _ => (ArticleKind.News, "/news", "News")
        };
    }

    public async Task<IActionResult> OnGetAsync([FromQuery(Name = "page")] int pageNumber = 1)
    {
        var (kind, basePath, plural) = SectionOf(Request.Path);
        var all = (await contentService.GetAsync()).Of(kind);
        TotalPages = Math.Max(1, (all.Count + PageSize - 1) / PageSize);
        if (pageNumber < 1 || pageNumber > TotalPages) return NotFound();
        PageNumber = pageNumber;
        Articles = all.Skip((pageNumber - 1) * PageSize).Take(PageSize).ToList();

        Heading = kind switch
        {
            ArticleKind.Blog => "Bike and car blogs",
            ArticleKind.ValueForMoney => "Value for money picks",
            _ => "Latest automotive news"
        };
        var suffix = pageNumber > 1 ? $" – Page {pageNumber}" : "";
        Meta.Title = $"{Heading}{suffix} | BikeCarHub";
        Meta.Description = kind switch
        {
            ArticleKind.Blog => "Buying guides, reviews and explainers on bikes, scooters, cars and EVs in India from BikeCarHub.",
            ArticleKind.ValueForMoney => "Which variant gives the most for the money? BikeCarHub's value-for-money picks for popular models.",
            _ => "Launches, price changes and updates from the Indian two-wheeler and car industry."
        };
        Meta.NoIndex = Request.Query.Keys.Any(k => k != "page");
        Meta.CanonicalPath = Meta.NoIndex || pageNumber == 1 ? basePath : $"{basePath}?page={pageNumber}";
        Meta.Crumbs((plural, basePath));
        return Page();
    }
}
