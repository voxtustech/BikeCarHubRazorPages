# Data mapping and data-quality report

Inspected on 13 Oct 2026 with SELECT statements only. Nothing in the database was changed.

- **Server:** SQL Server 2022 Web Edition, database `bikece93_BikeCarHubDB`, schema `BikeCarHubUser`.
- **Login:** has write permission (`db_datawriter`). Read-only behaviour is enforced by the site's write gate, not by the server.
- **Workbook:** `BikeCarHub.xlsx` was not present, so there is no workbook-to-database comparison. The mapping below is from the live schema.

## Table mapping
| Name in the brief | Real table | Rows | Used for |
|---|---|---|---|
| TWBrand | `TWBrands` | 44 | Brands (logo, description, title tag, meta description) |
| TwoWheeler | `TwoWheelers` | 355 | Models |
| TWVariant / VarientTest | `TWVarients` | 677 | Variants |
| TWSpec | `TWSpec` | 572 | Key specifications |
| TWMileage and Performance | `TWMileageAndPerformances` | 572 | Mileage |
| TWEngineAndTransmission | `TWEngineAndTransmissions` | 572 | Engine |
| TWFeatures | `TWFeatures` | 579 | Features |
| TWSafety | `TWSafety` | 572 | Safety |
| TWDimensionsAndCapacity | `TWDimensionsAndCapacities` | 572 | Dimensions |
| TWElectical | `TWElectricals` | 572 | Electricals |
| TWTyresAndBrake | `TWTyresAndBrakes` | 572 | Tyres and brakes |
| TWMotorAndBattery | `TWMotorAndBatteries` | 572 | Motor and battery |
| TWUnderpinning | `TWUnderpinnings` | 573 | Underpinnings |
| TWCharging | `TWChargings` | 1,144 | Charging |
| – | `TWChassisAndSuspensions` | 0 | Empty |
| – | 13 `TWEV…` tables | 105 each (210 in `TWEVPerformances`, `TWEVRanges`) | Electric variants |
| – | `TWImageColorPrices` | 0 | Empty: no per-variant price, colour or image |
| TWUpcomingBike | `TWUpcomingBikes` | 52 | Upcoming |
| LatestNews | `LatestNews` | 63 | News |
| – | `Blogs` 138, `ValueForMoney` 14, `HeroSlides` 3, `CompareItems` 18 | | Home and editorial |
| – | `AspNetUsers` 18, `NewsLetters` 32, `TWOrFWContactUs` 451 | | Account and forms |
| – | `Reviews` 4, `Wishlist` 4, `LoginModel` 0 | | Not used |

Joins are by `TwoWBrandId`, `TwoWheelerId` and `TWVarientId`; every spec table has foreign keys and indexes on the last two. There are no slug columns and no unique indexes.

## How the site handles each problem
| # | Problem | Count | What the site does |
|---|---|---|---|
| 1 | `BasePrice`/`TopPrice` empty although the text `Price` has a value | 163 of 355 models | Reads the rupee amounts out of the text `Price`; numeric columns are the fallback |
| 2 | `TopPrice` lower than `BasePrice` (Bajaj Platina 110, Honda Hornet 2.0, Hero PASSION Plus) | 3 | Shows lowest – highest of whatever is found |
| 3 | `LaunchDate` is free text that is not a date (year only, or blank) | 265 of 355 | Shows the text as stored; "newest" sort uses only parseable dates |
| 4 | Models with no variants and no `Type` (Tork Kratos X, Moto FAAST F4, F77) | 3 | Page shows "Specifications … not available yet" |
| 5 | Models with `IsActive = 0` (Honda CB1000R Plus, Hero Maestro Edge 125) | 2 | Left out of lists; their URL answers 410 Gone |
| 6 | Variants with no spec rows at all | 21 | Empty sections are hidden |
| 7 | `TWSpec` rows whose variant belongs to a different model | 21 | Specs are read by variant id only, so the model id on the row is ignored |
| 8 | More than one row per variant: `TWSpec` 16, `TWFeatures` 18, `TWUnderpinnings` 17, `TWChargings` 548, `TWEVPerformances` 105, `TWEVRanges` 105 | | First row (lowest id) is used |
| 9 | All upcoming bikes have an expected date in the past (latest 17 Dec 2025) | 52 of 52 | None is shown as upcoming. Listed as "Now launched" when a model of the same name exists, otherwise "Expected date has passed" |
| 10 | `TWUpcomingBikes.ImageURL` and `LatestNews.ImageURL` hold the page slug, not an image | all rows | Used as the slug. News images come from `ImageFolderURL`; upcoming bikes use the matching folder under `04UpcomingBike` when one exists (5 do) |
| 11 | Upcoming bikes with under 200 characters of detail | 28 of 52 | Page is `noindex` and out of the sitemap |
| 12 | Blogs with little or no body text in the database | 133 of 138 | See "Blog text" below |
| 13 | Value-for-money rows have no body column | 14 | Body comes from the article's `.docx` where one exists (5 do) |
| 14 | Hero slide prices stored as `?17.49 Lakh onwards` (₹ lost in a `varchar` column) | 3 | Leading `?` before a digit is shown as ₹ |
| 15 | Literal text `NULL` stored in summaries and details | several | Treated as empty |
| 16 | Non-breaking and doubled spaces in brand title tags | several | Whitespace is normalised |
| 17 | `TWOrFWContactUs.FormType` blank | 447 of 451 | Not read by the site |
| 18 | Model image paths use two prefixes: `/images/twowheeler/twowheelerimages/…` and `/images/twowheelerimages/…` | 206 / 148 | Only the first maps to a folder in `BCH` |

### Blog text
`Blogs.BlogDetail` never holds more than 500 characters, and 35 rows have none. The full articles are not in the database. For the five newest blogs the full text was found as a `.docx` beside the images in `BCH\Home\Images\02Blogs` and is shown from there. The other 133 blog pages show only the stored summary and are `noindex`. **If those pages bring search traffic today, their text needs to be supplied** (in `BlogDetail`, or as `.docx` files in the image folder).

## Images
Every image path in the database was checked against `BCH\Home\Images`.

| Kind | Rows | File present | Missing |
|---|---|---|---|
| Brand logos | 44 | 44 | 0 |
| Hero slides | 3 | 3 | 0 |
| Model images (active models) | 353 | 198 | 155 |
| Blog images | 138 | 5 | 133 |
| News images | 63 | 5 | 58 |
| Value for money | 14 | 5 | 9 |

The full list of the 355 missing paths is in [missing-images.txt](missing-images.txt). These files are missing from the local folder only. The previous site loads its images from `https://api.bikecarhub.com/images/...`, and the site now falls back to that host for any file that is not local (found on 13 Oct 2026 by reading the old home page; spot checks of a model, a blog and a compare image returned the image). `www.bikecarhub.com` itself answers every URL with its JavaScript app shell.

### Problems inside the `BCH` folder
- Nine files have the wrong extension (AVIF content): eight `.webp` files under Royal Enfield and `Triumph\Triumph SPEED 400\Phantom Black Storm Grey.png`. The site serves them with the correct type by reading the file header.
- Duplicate brand folders: `Bajaj` / `Bajaj Auto`, `TVS` / `TVS Motor Company`.
- Folder names that differ from the brand name or logo: `Aprilla`, `Hasqvarna`, `Motto Guzzi`, `Motto Morini`, `OJ Motor` (logo `QJMotor`), `Bounce` (logo `BounceInfinity`), `HarleyDavidson`. A `TorkMotors` logo has no image folder.
- Mixed depth: 908 images at brand\model\colour, 681 at brand\model\variant\colour, 2 loose in `Hasqvarna\`, 1 stray in `Triumph SPEED 400\`.
- Six file names end in a space before `.webp`; 20 contain `&`; one folder contains `°`.
- Empty folders: `00Header\01Home`, `02Blogs`, `03Compare`; all seven under `010Footer`; `09BCHEcosystem`.

## Assumptions
- Units are added only to numeric spec columns where the unit is standard: cc, km/l, L, mm, kg, km/h. Text columns are shown as stored.
- A spec value of 0, blank, `NULL`, `-` or `N/A` is treated as missing.
- Prices are treated as ex-showroom.
- The first variant by id is the default variant on a model page and in comparisons.
- "Latest launches" on the home page stands in for "featured": the database has no featured flag.
