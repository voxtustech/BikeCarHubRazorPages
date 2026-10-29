/*
  004 - Sitemap status, soft delete and last-modified columns.
  PROPOSED - NOT APPLIED. Review before running. Safe to run more than once.

  IsActive  : soft delete for editorial rows (TwoWheelers already has it). 0 = page answers 410 Gone.
  UpdatedDateTime : real "lastmod" for sitemaps; today the sitemap uses CreatedDateTime / Date.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('Blogs', 'IsActive') IS NULL
    ALTER TABLE Blogs ADD IsActive bit NOT NULL CONSTRAINT DF_Blogs_IsActive DEFAULT 1;
IF COL_LENGTH('LatestNews', 'IsActive') IS NULL
    ALTER TABLE LatestNews ADD IsActive bit NOT NULL CONSTRAINT DF_LatestNews_IsActive DEFAULT 1;
IF COL_LENGTH('ValueForMoney', 'IsActive') IS NULL
    ALTER TABLE ValueForMoney ADD IsActive bit NOT NULL CONSTRAINT DF_ValueForMoney_IsActive DEFAULT 1;
IF COL_LENGTH('TWUpcomingBikes', 'IsActive') IS NULL
    ALTER TABLE TWUpcomingBikes ADD IsActive bit NOT NULL CONSTRAINT DF_TWUpcomingBikes_IsActive DEFAULT 1;
IF COL_LENGTH('TWVarients', 'IsActive') IS NULL
    ALTER TABLE TWVarients ADD IsActive bit NOT NULL CONSTRAINT DF_TWVarients_IsActive DEFAULT 1;

IF COL_LENGTH('TwoWheelers', 'UpdatedDateTime') IS NULL     ALTER TABLE TwoWheelers     ADD UpdatedDateTime datetime2 NULL;
IF COL_LENGTH('TWVarients', 'UpdatedDateTime') IS NULL      ALTER TABLE TWVarients      ADD UpdatedDateTime datetime2 NULL;
IF COL_LENGTH('Blogs', 'UpdatedDateTime') IS NULL           ALTER TABLE Blogs           ADD UpdatedDateTime datetime2 NULL;
IF COL_LENGTH('LatestNews', 'UpdatedDateTime') IS NULL      ALTER TABLE LatestNews      ADD UpdatedDateTime datetime2 NULL;
IF COL_LENGTH('TWUpcomingBikes', 'UpdatedDateTime') IS NULL ALTER TABLE TWUpcomingBikes ADD UpdatedDateTime datetime2 NULL;

COMMIT TRANSACTION;
GO

IF OBJECT_ID('SitemapStatus', 'U') IS NULL
BEGIN
    -- One row per child sitemap, written by the site when it rebuilds them.
    CREATE TABLE SitemapStatus
    (
        SitemapName     varchar(60) NOT NULL CONSTRAINT PK_SitemapStatus PRIMARY KEY,
        UrlCount        int         NOT NULL,
        LastBuiltUtc    datetime2   NOT NULL,
        LastModifiedUtc datetime2   NULL
    );
END
GO
