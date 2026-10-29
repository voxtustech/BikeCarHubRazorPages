# BikeCarHub

Server-rendered ASP.NET Core 8 Razor Pages site for bikecarhub.com, reading the existing SQL Server database.
See [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) and [Docs/data-mapping-report.md](Docs/data-mapping-report.md).

## Prerequisites
- .NET SDK 8 or later (built and tested here with SDK 9.0.318 targeting `net8.0`; the .NET 8 runtime must be installed).
- Network access to the SQL Server.
- The `BCH\Home\Images` folder (images and article `.docx` files).

## Connection string
It is never stored in the repository. `appsettings.json` has an empty placeholder.

```
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;"
```

## Settings (`appsettings.json`)
| Key | Default | Meaning |
|---|---|---|
| `Site:BaseUrl` | `https://www.bikecarhub.com` | Used for canonical URLs, sitemaps and reset links |
| `Database:AllowWrites` | `false` | `false`: sign-up, password reset, newsletter and ask-a-question save nothing and say "Demo mode". `true`: they write to `AspNetUsers`, `NewsLetters`, `TWOrFWContactUs` |
| `Images:LocalRoot` | `BCH/Home/Images` | Folder that holds the images |
| `Images:RemoteBaseUrl` | `https://api.bikecarhub.com` | Host used for images that are not in the local folder |
| `Smtp:*` | empty | Outgoing mail for password-reset links. Empty: forgot-password says no email was sent |

## Run
**Visual Studio 2022:** open `BCHRazorPgaesClaude.sln`, set `BCHRazorPgaesClaude` as the startup project, press F5. User secrets are read automatically in Development.

**Command line:**
```
dotnet restore
dotnet build
dotnet test
dotnet run --project BCHRazorPgaesClaude.csproj
```

## Database migrations – review before running
Nothing in `Database/Migrations` has been applied. Each script is safe to run more than once.

| Script | Adds |
|---|---|
| `001_add_slug_columns.sql` | Nullable `Slug` columns and filtered unique indexes |
| `002_slug_history.sql` | `SlugHistory` table for redirects after a rename |
| `003_seo_override.sql` | `SeoOverride` table for hand-written titles and descriptions |
| `004_sitemap_status.sql` | `IsActive` / `UpdatedDateTime` columns and `SitemapStatus` |
| `005_listing_indexes.sql` | Indexes for listing and sort queries |

The site runs without them. Using the new columns needs a small code change after they exist.

## Adding cars later
1. Add a vehicle-type flag to the catalogue queries in `Data/CatalogRepository.cs` (or point a second repository at the car tables).
2. Add a `/new-cars` route to the pages in `Pages/Bikes` (they work on `Brand`/`Bike`/`Variant`, not on table names).
3. Add the car spec tables to the section lists in `Services/SpecService.cs` and the whitelist in `SqlCatalogRepository`.
4. Add a `cars` child sitemap in `Seo/SitemapEndpoints.cs`.

## Deploying to BigRock Windows hosting (IIS / Plesk)
1. In Plesk, check that the **ASP.NET Core 8 Hosting Bundle** is available for the domain. If only an older runtime is offered, publish self-contained (step 2, second command).
2. Publish:
   ```
   dotnet publish BCHRazorPgaesClaude.csproj -c Release -o publish
   ```
   Self-contained alternative: add `-r win-x86 --self-contained true` (or `win-x64`, matching the app pool).
   The publish output includes a generated `web.config` and the `BCH` image folder.
3. Upload the contents of `publish` to the site's `httpdocs` folder.
4. Set the connection string on the server as an environment variable or in Plesk's ASP.NET settings: name `ConnectionStrings__Default`. Do not put it in `appsettings.json`.
5. Give the app pool identity write permission on `Logs` and `App_Data` (sign-in cookies and reset links depend on the keys stored in `App_Data\keys`).
6. Turn on HTTPS for the domain. The site redirects to HTTPS and sends HSTS outside Development.
7. Check `/health`, `/robots.txt` and `/sitemap.xml`, then submit the sitemap in Search Console.

The current live site is a JavaScript single-page app that returns its shell for every URL, so its exact page addresses could not be read. The old database slugs (`blogs/…`, `latestnews/…`, `upcoming-bike/…`) are redirected with 301s; tell me any other address pattern the old site used and it can be added in `Program.cs`.
