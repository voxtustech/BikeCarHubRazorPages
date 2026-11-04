# SEO changelog: Google Search Console indexing fixes (3 Nov 2026)

Input: the GSC export of 221 not-indexed URLs (`not-indexed-urls-2026-11-02.csv`, crawled up to 25 Oct 2026).
Every one of those URLs was replayed against this build, running locally on the real database.
The result for each URL is in [gsc-url-check-2026-11-03.csv](gsc-url-check-2026-11-03.csv) (old URL, new status, final URL, robots tag).

## Result on the  221 URLs

| Outcome | URLs | Meaning |
|---|---|---|
| One 301 to an indexable page | 137 | Old address now lands, in a single hop, on its new clean page (`index,follow`, self canonical on `https://bikecarhub.com`) |
| Already the clean address, indexable | 6 | Served directly with 200 |
| 410 Gone | 20 | Removed models, old JSON/API endpoints, old admin login, old car spec pages |
| Lands on a page that stays `noindex` | 54 (50 by one 301, 4 directly) | 41 blogs, 10 value-for-money pages, 3 upcoming bikes: their text is missing from the database (see "Still open") |
| 301 to sign-in / sign-up (`noindex`, correct) | 4 | Old login and register links |
| 404 | 0 | |

Before this change the old site (a JavaScript app shell) answered every one of these URLs with 200 and the same empty page, which is why GSC reported soft 404s, duplicates and "crawled - currently not indexed".

## Changes

| # | File | What changed | Why | GSC error it fixes |
|---|---|---|---|---|
| 1 | `appsettings.json` | `Site:BaseUrl` changed from `https://www.bikecarhub.com` to `https://bikecarhub.com` | Google crawls and has indexed the bare domain; every canonical, `og:url`, sitemap `<loc>` and the robots.txt sitemap line pointed at `www`, so each crawled page declared a different URL as the real one | Alternative page with proper canonical tag; Duplicate, Google chose different canonical than user; Duplicate without user-selected canonical |
| 2 | `Program.cs` | New middleware: a request to the other spelling of the host (`www.bikecarhub.com` when the base URL is `bikecarhub.com`, or the reverse) gets a 301 to the same path on `Site:BaseUrl` | One host per site, so links to `www` consolidate onto the canonical host. Localhost and staging hosts are untouched | Duplicate / alternate canonical errors |
| 3 | `Seo/LegacyUrls.cs` (new), `Program.cs` | New `UseLegacyUrls()` middleware, placed before the lowercase redirect. It resolves every old address pattern straight to its final page in one 301, or answers 410. Replaces the ten `app.MapGet(... Redirect ...)` lines in `Program.cs`, which sent `/blogs/{slug}` etc. to a guessed slug that could then redirect again or 404 | One hop per old URL, no redirect chains, no redirect to a page that does not exist | Soft 404; Page with redirect; Crawled/Discovered - currently not indexed |
| 3a | | Old brand folders with spaces or the old slug style: `/royal enfield/royal-enfield-himalyan`, `/ola electric/ola-roadster`, `/ktm/ktm-200-duke`, `/bmw` → `/new-bikes/{brand}/{model}` or `/new-bikes/{brand}`. Model matched by its slug, brand+model slug or full name. A model switched off (`IsActive = 0`) → 410. A model the site never had → real 404 | 22 "brand folder with a space" + 59 "bike model page" + 9 "brand page" rows | Soft 404; Crawled - currently not indexed; Not found (404) |
| 3b | | Old ASP.NET routes: `/Home/TwoWheelerDetails?modelId=N` → that model's page (by `TwoWheelerId`), `/Home/TWBrandDetails?brandId=N` → that brand's page, `/LatestNews/Index` → `/news`, `/ValueForMoney` → `/value-for-money`, `/UpcomingBike/TWUpcomingBikeDetails` → `/upcoming-bikes`, `/compare/twcompare` → `/compare`, `/Account/Login`, `/login` → `/account/sign-in`, `/register` → `/account/sign-up` | The ids in the old query strings map to real records | Alternative page with proper canonical tag; Crawled - currently not indexed |
| 3c | | Old endpoints and removed features answer **410 Gone**: `/Home/Get…`, `/Compare/Get…`, `/Wishlist/Get…`, `/{brand}/GetTWSpecData`, `/AdminLogin/…`, `/my-reviews`, `/…-features-and-specifications` (old car spec pages; the site has no car pages yet) | API endpoints should not answer a plain GET with a page; 410 makes Google drop them faster than 404. They are not blocked in robots.txt, so Google can see the 410 | Not found (404); Server error (5xx); Page with redirect |
| 3d | | Old article sections: `/blogs/…`, `/latestnews/…`, `/latest-news/…` → `/blog/…`, `/news/…`; `/value-for-money/marutisuzukibrezza` (old compact slug) → `/value-for-money/maruti-suzuki-brezza-the-value-for-money-variant`. Unknown old article → 410 | 43 blog, 26 news, 11 value-for-money rows had capitals, `₹`, commas, colons and en dashes, or a second slug style | Soft 404; Crawled - currently not indexed; Duplicate without user-selected canonical |
| 3e | | Small static map: `/contact-us`, `/ask-question` → `/ask-a-question`; `/privacy-policy` → `/privacy`; `/terms-and-conditions` → `/terms`; `/brands` → `/new-bikes`; `/blogs` → `/blog`; `/latestnews`, `/latest-news` → `/news`; `/upcoming-bike` → `/upcoming-bikes` | The "new site page, not yet crawled" rows | Discovered - currently not indexed |
| 3f | | `/upcoming-bike/{slug}` → the upcoming page, or, when the bike has launched and the preview has no write-up, straight to the model page | | Soft 404 (12 upcoming rows) |
| 4 | `Services/Slugger.cs` | New `Slugger.Match()`: finds the record an old slug meant by exact slug, then cleaned slug, then the same letters without hyphens (`mahindraBE-formulaE-edition`, `18,000` vs `18000`), then a unique prefix (`marutisuzukibrezza`). Returns nothing when two records fit equally, so it never guesses | Old URLs used several slug styles | Soft 404; Duplicate without user-selected canonical |
| 5 | `Pages/Articles/Detail.cshtml.cs`, `Pages/Upcoming/Detail.cshtml.cs` | Use `Slugger.Match()` for wrong slugs on the current sections too (301 to the clean address). Upcoming bike that has launched and has under 200 characters of text → 301 to its model page | Same matching rules everywhere; thin previews of launched bikes were soft-404 candidates | Soft 404 |
| 6 | `Pages/Status.cshtml` | Opened directly (for example `/status/404`), the error page now answers with that status code instead of 200. 410 message made general ("This page is no longer available") | An error page answering 200 is a soft 404 | Soft 404 |
| 7 | `Program.cs` | `/ask-a-question` removed from the `X-Robots-Tag: noindex` list; `/wishlist` added | `/ask-a-question` is in the sitemap and its page says `index`, but the header said `noindex`, a contradiction GSC reports. The wishlist is personal | Submitted URL marked noindex |
| 8 | `Services/ContentService.cs` | Summaries are stripped of the HTML tags the old editor left in them (`<span class="font-weight-700">`) and their entities decoded | These tags showed as literal text on the page and in the meta description | Crawled - currently not indexed (page quality) |
| 9 | `tests/BikeCarHub.Tests/CoreTests.cs` | 9 cases for `Slugger.Match()` built from real GSC URLs | | |
| 10 | `README.md` | `Site:BaseUrl` default and the host redirect documented | | |

## Verified

- `dotnet build`: succeeded, 0 errors (78 existing nullable warnings in `Models/`).
- `dotnet test`: 54 passed, 0 failed.
- Ran the build on `http://localhost:5199` against the real database and requested all 221 GSC URLs with curl (results file above). Every redirect is a single hop.
- `Host: www.bikecarhub.com` → `301 https://bikecarhub.com/...` (query string kept).
- `/robots.txt` lists `Sitemap: https://bikecarhub.com/sitemap.xml`; sitemap `<loc>` values use `https://bikecarhub.com`.

## Still open (not fixable in code)

1. **Article text is missing for 54 pages.** 41 blogs, 10 value-for-money pages and 3 upcoming bikes have under 200 characters of body text in the database and no `.docx` file, so they stay `noindex` and out of the sitemap on purpose: indexing a summary-only page would just turn "soft 404" into "crawled - currently not indexed". Filter `gsc-url-check-2026-11-03.csv` on `Robots = noindex,follow` for the list. Supplying the text (in `Blogs.BlogDetail`, or as a `.docx` in the article's image folder) makes each page indexable automatically and adds it to the sitemap.
2. **Deploy and host binding.** Both `bikecarhub.com` and `www.bikecarhub.com` must point at this site in IIS for the host redirect to work. After deploying, resubmit `https://bikecarhub.com/sitemap.xml` in GSC and use "Validate fix" on each error group. The GSC property should be the bare domain or a Domain property.
3. **A few models kept their old typo** in the new slug (for example `/new-bikes/royal-enfield/himalyan`), because slugs come from `TwoWheelerName`. Correcting the name in the database changes the URL, and the misspelt address would then 404, so if you fix a name, add a redirect for its old slug at the same time.
