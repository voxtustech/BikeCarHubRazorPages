/*
  001 - Slug columns and unique indexes.  PROPOSED - NOT APPLIED. Review before running.
  Safe to run more than once. Adds nullable columns only; no existing data is changed.

  The site currently builds slugs in code from the names. Once these columns are filled
  (script 006, to be written after you approve the slug values), the code can read them instead,
  and a renamed model keeps its address through SlugHistory (script 002).
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('TWBrands', 'Slug') IS NULL        ALTER TABLE TWBrands        ADD Slug varchar(120) NULL;
IF COL_LENGTH('TwoWheelers', 'Slug') IS NULL     ALTER TABLE TwoWheelers     ADD Slug varchar(160) NULL;
IF COL_LENGTH('TWVarients', 'Slug') IS NULL      ALTER TABLE TWVarients      ADD Slug varchar(160) NULL;
IF COL_LENGTH('Blogs', 'Slug') IS NULL           ALTER TABLE Blogs           ADD Slug varchar(200) NULL;
IF COL_LENGTH('LatestNews', 'Slug') IS NULL      ALTER TABLE LatestNews      ADD Slug varchar(200) NULL;
IF COL_LENGTH('ValueForMoney', 'Slug') IS NULL   ALTER TABLE ValueForMoney   ADD Slug varchar(200) NULL;
IF COL_LENGTH('TWUpcomingBikes', 'Slug') IS NULL ALTER TABLE TWUpcomingBikes ADD Slug varchar(200) NULL;

COMMIT TRANSACTION;
GO

-- Filtered unique indexes: uniqueness is enforced only once a slug has been filled in.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TWBrands_Slug')
    CREATE UNIQUE INDEX UX_TWBrands_Slug ON TWBrands (Slug) WHERE Slug IS NOT NULL;

-- A model slug is unique within its brand; a variant slug within its model.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TwoWheelers_Brand_Slug')
    CREATE UNIQUE INDEX UX_TwoWheelers_Brand_Slug ON TwoWheelers (TwoWBrandId, Slug) WHERE Slug IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TWVarients_Model_Slug')
    CREATE UNIQUE INDEX UX_TWVarients_Model_Slug ON TWVarients (TwoWheelerId, Slug) WHERE Slug IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Blogs_Slug')
    CREATE UNIQUE INDEX UX_Blogs_Slug ON Blogs (Slug) WHERE Slug IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_LatestNews_Slug')
    CREATE UNIQUE INDEX UX_LatestNews_Slug ON LatestNews (Slug) WHERE Slug IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ValueForMoney_Slug')
    CREATE UNIQUE INDEX UX_ValueForMoney_Slug ON ValueForMoney (Slug) WHERE Slug IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TWUpcomingBikes_Slug')
    CREATE UNIQUE INDEX UX_TWUpcomingBikes_Slug ON TWUpcomingBikes (Slug) WHERE Slug IS NOT NULL;
GO
