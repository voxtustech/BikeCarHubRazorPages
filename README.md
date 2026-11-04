# BikeCarHub

Server-rendered ASP.NET Core 8 Razor Pages site for bikecarhub.com, reading the existing SQL Server database.
See [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) and [Docs/data-mapping-report.md](Docs/data-mapping-report.md).

## Prerequisites
- .NET SDK 8 or later (built and tested here with SDK 9.0.318 targeting `net8.0`; the .NET 8 runtime must be installed).
- Network access to the SQL Server.
- The `wwwroot\images` folder (images) and the `Articles` folder (article `.docx` files).

## Connection string
It is never stored in the repository. `appsettings.json` has an empty placeholder.

```
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;"
```

## Settings (`appsettings.json`)
| Key | Default | Meaning |
|---|---|---|
| `Site:BaseUrl` | `https://bikecarhub.com` | Used for canonical URLs, sitemaps and reset links. Requests to the other spelling (www or bare) get a 301 to this host |
| `Database:AllowWrites` | `false` | `false`: sign-up, password reset, newsletter and ask-a-question save nothing and say "Demo mode". `true`: they write to `AspNetUsers`, `NewsLetters`, `TWOrFWContactUs` |
| `Images:LocalRoot` | `wwwroot/images` | Folder that holds the images |
| `Images:ArticlesRoot` | `Articles` | Private folder that holds the article `.docx` files. A new article with no database row is published by adding an entry to `Articles\articles.json` (the file explains each field): its Word file is laid out automatically (Heading 1 = H1, first paragraph = intro, first picture = cover, tables become stat strips, image-and-text blocks, quotes, callouts or data tables, a "Frequently asked questions" section becomes FAQs). Pictures inside the Word file must also be copied, unchanged, into the entry's `imageFolder`. Each file sits at the same sub-path as the article's images, for example `wwwroot\images\blogs\2026\Sept\Kia Sorento` and `Articles\blogs\2026\Sept\Kia Sorento` |
| `Images:RemoteBaseUrl` | empty | Optional host for images that are not in the local folder. Empty: those images show the default picture |
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
   The publish output includes a generated `web.config`, the images in `wwwroot\images` and the `Articles` folder.
3. Upload the contents of `publish` to the site's `httpdocs` folder **with the site offline**, otherwise IIS shows "HTTP Error 503.0, 0x800704e7" (see below):
   1. Upload `Deploy\app_offline.htm` to the root of `httpdocs` first. IIS stops the app and shows that page to visitors.
   2. Upload the contents of `publish`, overwriting the old files.
   3. Delete `app_offline.htm` from `httpdocs`. The app starts on the next request.
4. Set the connection string on the server as an environment variable or in Plesk's ASP.NET settings: name `ConnectionStrings__Default`. Do not put it in `appsettings.json`.
5. Give the app pool identity write permission on `Logs` and `App_Data` (sign-in cookies and reset links depend on the keys stored in `App_Data\keys`).
6. Turn on HTTPS for the domain. The site redirects to HTTPS and sends HSTS outside Development.
7. Check `/health`, `/robots.txt` and `/sitemap.xml`, then submit the sitemap in Search Console.

### "HTTP Error 503.0 Service Unavailable" (0x800704e7) after publishing
`0x800704e7` means the ASP.NET Core module saw the app shutting down. Overwriting the DLLs and `web.config` of a running site does that: every changed file restarts the app, starts during a half-finished upload crash, and after a few crashes IIS's rapid-fail protection stops the application pool, so the 503 stays until the pool is started again. To recover:
1. Make sure the upload has finished and no `app_offline.htm` is left in `httpdocs`.
2. In Plesk, open **Websites & Domains → bikecarhub.com → Dedicated IIS Application Pool** (or **IIS Application Pool**) and click **Recycle** (or **Start** if it shows stopped).
3. If it still fails, set `stdoutLogEnabled="true"` in the server's `web.config`, load the site once, read `logs\stdout_*.log` (create the `logs` folder if it is missing) and the files in `Logs\`, then set it back to `false`.
Using `app_offline.htm` for every upload (step 3 above) prevents it.

The current live site is a JavaScript single-page app that returns its shell for every URL, so its exact page addresses could not be read. The old database slugs (`blogs/…`, `latestnews/…`, `upcoming-bike/…`) are redirected with 301s; tell me any other address pattern the old site used and it can be added in `Program.cs`.
