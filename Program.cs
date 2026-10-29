using System.Threading.RateLimiting;
using BikeCarHub.Data;
using BikeCarHub.Seo;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(context.HostingEnvironment.ContentRootPath, "Logs", "log-.txt"),
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

builder.Services.AddRazorPages(options =>
{
    // One list page and one article page serve news, blogs and value-for-money.
    options.Conventions.AddPageRoute("/Articles/List", "blog");
    options.Conventions.AddPageRoute("/Articles/List", "value-for-money");
    options.Conventions.AddPageRoute("/Articles/Detail", "blog/{slug}");
    options.Conventions.AddPageRoute("/Articles/Detail", "value-for-money/{slug}");
});
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.ConstraintMap["facet"] = typeof(FacetConstraint);
});
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.AddHealthChecks().AddCheck<DbHealthCheck>("database");
builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(180));

builder.Services.AddDataProtection()
    .SetApplicationName("BikeCarHub")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/sign-in";
        options.AccessDeniedPath = "/account/sign-in";
        options.Cookie.Name = "bch_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

// Only POSTs count, so reading a form page is never throttled.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("forms", context => HttpMethods.IsPost(context.Request.Method)
        ? RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) })
        : RateLimitPartition.GetNoLimiter("read"));
});

builder.Services.AddSingleton<SiteSettings>();
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<IWriteGate, ConfigWriteGate>();
builder.Services.AddSingleton<ICacheStore, MemoryCacheStore>();
builder.Services.AddSingleton<IImageUrlProvider, ImageUrlProvider>();
builder.Services.AddSingleton<ICatalogRepository, SqlCatalogRepository>();
builder.Services.AddSingleton<IContentRepository, SqlContentRepository>();
builder.Services.AddSingleton<IUserRepository, SqlUserRepository>();
builder.Services.AddSingleton<IFormRepository, SqlFormRepository>();
builder.Services.AddSingleton<ICatalogService, CatalogService>();
builder.Services.AddSingleton<ISpecService, SpecService>();
builder.Services.AddSingleton<IElectricCatalog, ElectricCatalog>();
builder.Services.AddSingleton<IWishlistRepository, SqlWishlistRepository>();
builder.Services.AddSingleton<IContentService, ContentService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/status/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/status/{0}");
app.UseHttpsRedirection();
app.UseResponseCompression();

var remoteImages = app.Configuration["Images:RemoteBaseUrl"]?.TrimEnd('/');
var imageSources = string.IsNullOrWhiteSpace(remoteImages) ? "'self' data:" : $"'self' data: {remoteImages}";
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "DENY";
    headers["Content-Security-Policy"] =
        $"default-src 'self'; img-src {imageSources}; style-src 'self'; script-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // Versioned URLs (asp-append-version) never change, so they can be cached for a year.
        var versioned = context.Context.Request.Query.ContainsKey("v");
        context.Context.Response.Headers.CacheControl = versioned ? "public,max-age=31536000,immutable" : "public,max-age=86400";
    }
});

// One URL per page: lowercase, no trailing slash.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";
    var isPage = HttpMethods.IsGet(context.Request.Method) && !path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase);
    if (isPage && path.Length > 1)
    {
        var canonical = path.TrimEnd('/').ToLowerInvariant();
        if (canonical.Length == 0) canonical = "/";
        if (!string.Equals(canonical, path, StringComparison.Ordinal))
        {
            context.Response.Redirect(canonical + context.Request.QueryString, permanent: true);
            return;
        }
    }
    await next();
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

string[] privatePrefixes = ["/account", "/search", "/newsletter", "/ask-a-question", "/compare/go", "/status"];
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isPrivate = privatePrefixes.Any(p => path.StartsWithSegments(p));
    if (isPrivate) context.Response.Headers["X-Robots-Tag"] = "noindex";
    context.Response.OnStarting(() =>
    {
        var response = context.Response;
        if (HttpMethods.IsGet(context.Request.Method) && response.StatusCode == 200
            && response.ContentType?.StartsWith("text/html") == true && !response.Headers.ContainsKey("Cache-Control"))
        {
            var cacheable = !isPrivate && context.User.Identity?.IsAuthenticated != true;
            response.Headers.CacheControl = cacheable ? "public,max-age=300" : "no-store";
        }
        return Task.CompletedTask;
    });
    await next();
});

app.MapRazorPages();
app.MapHealthChecks("/health");
app.MapSeoEndpoints();

app.MapGet("/images/{**path}", (string path, IImageUrlProvider images, HttpContext context) =>
{
    var file = images.MapToLocalFile("/images/" + path);
    if (file is null) return Results.NotFound();
    context.Response.Headers.CacheControl = "public,max-age=604800";
    return Results.File(file, ImageUrlProvider.SniffContentType(file), lastModified: File.GetLastWriteTimeUtc(file));
});

// Addresses used by the previous site, kept alive with permanent redirects.
app.MapGet("/contact-us", () => Results.Redirect("/ask-a-question", permanent: true));
app.MapGet("/privacy-policy", () => Results.Redirect("/privacy", permanent: true));
app.MapGet("/terms-and-conditions", () => Results.Redirect("/terms", permanent: true));
app.MapGet("/login", () => Results.Redirect("/account/sign-in", permanent: true));
app.MapGet("/blogs", () => Results.Redirect("/blog", permanent: true));
app.MapGet("/blogs/{slug}", (string slug) => Results.Redirect($"/blog/{Slugger.Slugify(slug)}", permanent: true));
app.MapGet("/latestnews", () => Results.Redirect("/news", permanent: true));
app.MapGet("/latestnews/{slug}", (string slug) => Results.Redirect($"/news/{Slugger.Slugify(slug)}", permanent: true));
app.MapGet("/upcoming-bike", () => Results.Redirect("/upcoming-bikes", permanent: true));
app.MapGet("/upcoming-bike/{slug}", (string slug) => Results.Redirect($"/upcoming-bikes/{Slugger.Slugify(slug)}", permanent: true));

app.Run();

public partial class Program;
