using BikeCarHub.Domain;
using BikeCarHub.Seo;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;

namespace BikeCarHub.Pages.Articles;

public class DetailModel(IContentService contentService, IImageUrlProvider images, SiteSettings site) : BasePageModel
{
    public Article Article { get; private set; } = null!;
    public IReadOnlyList<Article> Related { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var (kind, basePath, plural) = ListModel.SectionOf(Request.Path);
        var all = (await contentService.GetAsync()).Of(kind);
        var article = all.FirstOrDefault(a => a.Slug == slug);
        if (article is null)
        {
            // Old links carried symbols and capitals; send them to the clean address.
            var normalized = Slugger.Slugify(slug);
            var match = all.FirstOrDefault(a => a.Slug == normalized);
            return match is not null ? Moved(match.Url) : NotFound();
        }

        Article = article;
        Related = all.Where(a => a != article).Take(3).ToList();

        var image = images.Resolve(article.ImagePath);
        Meta.Title = $"{Formatting.Truncate(article.Title, 70)} | BikeCarHub";
        Meta.Description = Formatting.Truncate(article.Summary ?? article.Title, 160);
        Meta.CanonicalPath = article.Url;
        Meta.NoIndex = !article.IsIndexable;
        Meta.OgType = "article";
        Meta.Image = image;
        Meta.PreloadImage = image;
        Meta.Crumbs((plural, basePath), (article.Title, article.Url));
        if (article.IsIndexable) Meta.JsonLd.Add(JsonLd.ForArticle(site, article, image));
        return Page();
    }
}
