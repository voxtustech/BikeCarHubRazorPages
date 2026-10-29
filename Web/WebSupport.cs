using BikeCarHub.Seo;
using BikeCarHub.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BikeCarHub.Web;

public abstract class BasePageModel : PageModel
{
    public PageMeta Meta
    {
        get
        {
            if (ViewData["Meta"] is not PageMeta meta) ViewData["Meta"] = meta = new PageMeta();
            return meta;
        }
    }

    protected IActionResult Gone() => StatusCode(StatusCodes.Status410Gone);

    protected IActionResult Moved(string url) => RedirectPermanent(url);
}

/// <summary>Brand facet segments that are landing pages rather than model slugs.</summary>
public sealed class FacetConstraint : IRouteConstraint
{
    public static readonly string[] Facets = ["bikes", "scooters", "electric", "petrol"];

    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) && value is string s && Facets.Contains(s, StringComparer.OrdinalIgnoreCase);
}

public static class HtmlExtensions
{
    /// <summary>Image tag with fixed dimensions (no layout shift), lazy loading below the fold, and a resolved URL.</summary>
    public static IHtmlContent Img(this IHtmlHelper html, string? path, string alt, int width, int height, bool eager = false, string? css = null, bool highPriority = true)
    {
        var images = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IImageUrlProvider>();
        var tag = new TagBuilder("img") { TagRenderMode = TagRenderMode.SelfClosing };
        tag.Attributes["src"] = images.Resolve(path);
        tag.Attributes["alt"] = alt;
        tag.Attributes["width"] = width.ToString();
        tag.Attributes["height"] = height.ToString();
        tag.Attributes["data-fallback"] = images.Placeholder;
        if (!eager)
        {
            tag.Attributes["loading"] = "lazy";
            tag.Attributes["decoding"] = "async";
        }
        else if (highPriority) tag.Attributes["fetchpriority"] = "high";
        if (css is not null) tag.AddCssClass(css);
        return tag;
    }

    public static string DateText(this IHtmlHelper _, DateTime? date) => (date?.ToString("d MMM yyyy") ?? "").Replace(" Sep ", " Sept ");

    /// <summary>Current URL with one query value replaced, used for pagination and sort links.</summary>
    public static string WithQuery(this HttpRequest request, string key, string? value)
    {
        var query = request.Query.Where(q => !q.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(q => q.Key, q => (string?)q.Value.ToString());
        if (!string.IsNullOrEmpty(value)) query[key] = value;
        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(request.Path, query.Where(q => !string.IsNullOrEmpty(q.Value)));
    }
}
