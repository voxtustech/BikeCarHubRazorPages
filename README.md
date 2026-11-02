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

### Read-only and write logins
The site reads with `ConnectionStrings:Default` and writes (sign-up, password reset, newsletter, ask a question, wishlist) with `ConnectionStrings:Write`. When `Write` is empty, writes use `Default`.

Recommended setup:
1. In Plesk, add a second database user for the BikeCarHub database with a **read-only** role and put it in `ConnectionStrings:Default`.
2. Put the existing read-write user in `ConnectionStrings:Write`, and only when `Database:AllowWrites` is `true`.

```
dotnet user-secrets set "ConnectionStrings:Write" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;"
```
On the server the names are `ConnectionStrings__Default` and `ConnectionStrings__Write`.

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
| `006_wishlist_unique.sql` | One wishlist row per account and model |
| `007_fill_slugs.sql` | Not in the repo: generated from your data (see below) |

The site runs with or without them.

### Stored slugs (keep URLs when names change)
Today every URL is built from the brand, model and variant names, so renaming a model in the database changes its address. To fix the addresses:
1. Run `001_add_slug_columns.sql` and `002_slug_history.sql`.
2. Run the site in Development against the database and open `/dev/slug-fill.sql`. It returns an `UPDATE` per brand, model and variant with the slug the site uses today. Save it as `Database/Migrations/007_fill_slugs.sql`, review it, then run it. It only fills empty slugs, so it is safe to run again.
3. Nothing else is needed: the site reads the `Slug` columns as soon as they exist and uses a stored slug in preference to a computed one. A computed slug never takes a slug that another row has stored.

After that, a name can change without moving the page. To change a page's address on purpose, update its `Slug` and add the old path to `SlugHistory`:
```sql
INSERT INTO SlugHistory (EntityType, EntityId, OldPath) VALUES ('model', 123, '/new-bikes/bajaj/old-slug');
UPDATE TwoWheelers SET Slug = 'new-slug' WHERE TwoWheelerId = 123;
```
`EntityType` is `brand`, `model` or `variant`. The old address, and every page under it (variants, brand facets), answers a 301 to the new one within 30 minutes (the catalogue cache).

## Images that are only on the old host
155 model images and most blog and news images are not in `BCH\Home\Images` and are loaded from `api.bikecarhub.com` (list in `Docs/missing-images.txt`). Before the old site and its image host are switched off, copy them:
```
powershell -ExecutionPolicy Bypass -File tools\Download-MissingImages.ps1
```
Files go to `BCH\Home\Images\_remote\<same path>`, and the site serves them from there instead of the old host. The script skips files it already has and writes any failures to `tools\missing-images-failed.txt`.

Only image files (`.webp`, `.avif`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`) are served under `/images`; the article `.docx` files in the same folders are not downloadable.

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
