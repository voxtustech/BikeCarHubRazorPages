# BikeCarHub – architecture

## Stack
- ASP.NET Core 8 Razor Pages. Every public page is rendered on the server; the one JavaScript file (`wwwroot/js/site.js`) only enhances pages that already work without it.
- **Dapper** over `Microsoft.Data.SqlClient`: the existing schema has wide, loosely typed spec tables that are read as-is, which Dapper does without an entity model per table.
- `IMemoryCache` behind `ICacheStore`; Serilog to console and `Logs/log-YYYYMMDD.txt`.
- One web project, organised by folder. A second project holds the unit tests.

| Folder | Holds |
|---|---|
| `Domain/` | Plain models: `Brand`, `Bike`, `Variant`, `Article`, `UpcomingBike` |
| `Data/` | Connection factory, write gate, Dapper repositories (all SQL lives here) |
| `Services/` | Catalogue, specs, content, slugs, formatting, images, EMI, auth, email |
| `Seo/` | `PageMeta`, JSON-LD builders, robots.txt and sitemap endpoints |
| `Web/` | Base page model, route constraint, HTML helpers |
| `Pages/` | Razor Pages (no SQL, no business rules) |
| `Database/Migrations/` | Proposed SQL scripts – **none have been run** |
| `tests/BikeCarHub.Tests/` | xUnit tests |

## Data flow
`Page → Service (cached) → Repository → SQL Server`

- **Catalogue** (brands, models, variants) is loaded once into an immutable `Catalog` snapshot and cached for 30 minutes. Listing, filtering, sorting and search run in memory over 355 models.
- **Specs** for a variant are read in one round trip (one `SELECT TOP 1 *` per spec table) and cached per variant for 30 minutes. Table names come from a fixed list in code; the variant id is a parameter.
- **Editorial** (blogs, news, value for money, upcoming, hero slides, compare topics) is cached for 10 minutes.
- **Writes** (sign-up, password reset, newsletter, ask a question) go through `IWriteGate`. With `Database:AllowWrites` = `false` (the default) no statement that changes data is ever sent; the forms answer with a labelled "Demo mode" message.

## URL map
| URL | Page |
|---|---|
| `/` | Home |
| `/new-bikes` | Listing with filters, sort, `?page=N` |
| `/new-bikes/{brand}` | Brand |
| `/new-bikes/{brand}/{bikes\|scooters\|electric\|petrol}` | Brand + type / fuel landing page |
| `/new-bikes/{brand}/{model}` | Model detail (first variant's specs) |
| `/new-bikes/{brand}/{model}/{variant}` | Variant detail |
| `/compare`, `/compare/{a}-vs-{b}[-vs-{c}]` | Compare |
| `/upcoming-bikes`, `/upcoming-bikes/{slug}` | Upcoming |
| `/news`, `/news/{slug}` | News |
| `/blog`, `/blog/{slug}` | Blogs |
| `/value-for-money`, `/value-for-money/{slug}` | Value for money |
| `/emi-calculator`, `/ask-a-question`, `/about`, `/privacy`, `/terms`, `/disclaimer` | Tools and static pages |
| `/account/sign-in`, `/sign-up`, `/forgot-password`, `/reset-password`, `/sign-out` | Account |
| `/robots.txt`, `/sitemap.xml`, `/sitemaps/{name}-{n}.xml`, `/health` | Machine endpoints |
| `/used-bikes/...` | Reserved for the marketplace; not built (no listing tables exist) |

Slugs are built in code from names because the database has no slug columns yet (see migration 001). A model's slug is its name without the brand words; a clash inside a brand gets the record id appended.

## SEO rules
- One title, description, H1, canonical, Open Graph/Twitter set and breadcrumb trail per page, from `PageMeta`.
- JSON-LD from real data only: `Organization` (home), `BreadcrumbList`, `Product` with `AggregateOffer` only when a price exists, `Article`/`NewsArticle`. No ratings.
- **Indexable facets:** brand, brand+model, brand+type, brand+fuel, and only where they have results. Every other filter, sort or price query is `noindex,follow` with the canonical pointing at the clean parent. `?page=N` is self-canonical.
- **Status codes:** 301 for a differently written slug, the old site's `/blogs/…`, `/latestnews/…`, `/upcoming-bike/…`, uppercase or trailing-slash URLs, and unordered compare pairs; 410 for models with `IsActive = 0`; 404 for anything unknown. Error pages carry no canonical.
- **Thin content:** an article or upcoming page with under 200 characters of text is `noindex` and left out of the sitemap.
- Sitemap index with child sitemaps (pages, brands, models, variants, upcoming, news, blogs, value-for-money), 10,000 URLs per file, `lastmod` where a date exists, cached 30 minutes.

## Images
`IImageUrlProvider` maps a database path (`/images/...`) to a folder under `Images:LocalRoot` (the `BCH/Home/Images` layout) and serves it from `/images/{path}`, with the content type read from the file itself. A path with no local file gets the placeholder, so a broken image is never rendered. A path with no local file is requested from `Images:RemoteBaseUrl`, which is set to `https://api.bikecarhub.com` – the host the previous site loaded its images from. If that also fails, the browser swaps in the placeholder.

## Home page
The home page has its own layout (`Pages/Shared/_HomeLayout.cshtml`), stylesheet (`wwwroot/css/home.css`) and script (`wwwroot/js/home.js`). Section order: hero with the bike finder and featured showcase, budget and type tiles, latest launches, brands, head-to-head comparisons, EMI mini calculator, stories and news, launch radar, question and newsletter band. Every count shown is computed from the catalogue. Other pages still use `_Layout.cshtml` and `site.css`.

## Caching and performance
- HTML for anonymous visitors: `Cache-Control: public, max-age=300`. Account, form and search pages: `no-store`.
- Static files: one year when versioned (`?v=`), one day otherwise. Images: seven days.
- Brotli/gzip response compression. One CSS file, one deferred JS file, no third-party requests.
- Every image has width and height; below-the-fold images are lazy; the main image of a page is preloaded.

## Security
Parameterised SQL only · anti-forgery on every POST · HTTPS redirect and HSTS outside Development · CSP (`default-src 'self'`, no inline script or style), `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options` · rate limit of 5 POSTs a minute per IP on all forms · honeypot field on public forms · cookie auth against the existing `AspNetUsers` hashes · sign-in never writes · reset tokens are time-limited (1 hour) and tied to the user's security stamp.

## Not done in this phase
- Responsive `srcset` and WebP generation (source images exist in one size only).
- SlugHistory lookups, SEO overrides and SitemapStatus (their tables are proposed, not created).
- A local/seed data fallback: the hosted database was reachable, so it was not needed. If the database is down the site answers with the error page.
- Reviews and wishlist (tables exist; outside the Phase 1 page list).

## Later phases
- **Cars:** add `VehicleType` to the catalogue query and a `/new-cars` route group; services and pages already work on `Bike`/`Variant` shapes rather than two-wheeler tables.
- **Azure:** Blob Storage + CDN behind `IImageUrlProvider`; Redis behind `ICacheStore`; Azure AI Search for free-text search.
- **Used-vehicle marketplace** under `/used-bikes`.
- **AI ideas:** natural-language bike finder over the spec tables, generated comparison summaries (clearly labelled), duplicate/typo detection for the data team.
