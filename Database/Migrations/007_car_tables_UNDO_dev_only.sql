/*
  007 UNDO - removes everything 007_car_tables.sql added, INCLUDING ANY CAR DATA in those tables.
  Dev only: it refuses to run unless the database name ends in Dev.
  Bike tables and existing brand rows are not touched. Only the 3 new TWBrands columns are removed.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_NAME() NOT LIKE '%Dev'
BEGIN
    RAISERROR('STOPPED: this undo script only runs on the Dev database. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO

SELECT DB_NAME() AS RunningOnDatabase;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DROP VIEW IF EXISTS dbo.vw_FWKeySpecs;

-- Child tables first, because of the foreign keys.
DROP TABLE IF EXISTS dbo.FWImages;
DROP TABLE IF EXISTS dbo.FWVarientColours;
DROP TABLE IF EXISTS dbo.FWColours;
DROP TABLE IF EXISTS dbo.FWNcapRatings;
DROP TABLE IF EXISTS dbo.FWSources;
DROP TABLE IF EXISTS dbo.FWFeatures;
DROP TABLE IF EXISTS dbo.FWSafety;
DROP TABLE IF EXISTS dbo.FWBodyAndChassis;
DROP TABLE IF EXISTS dbo.FWEVBatteries;
DROP TABLE IF EXISTS dbo.FWPowertrains;
DROP TABLE IF EXISTS dbo.FWVarients;
DROP TABLE IF EXISTS dbo.FourWheelers;
DROP TABLE IF EXISTS dbo.BrandProfiles;

DROP INDEX IF EXISTS UX_TWBrands_BrandSlug ON dbo.TWBrands;
IF COL_LENGTH('dbo.TWBrands', 'BrandSlug') IS NOT NULL       ALTER TABLE dbo.TWBrands DROP COLUMN BrandSlug;
IF COL_LENGTH('dbo.TWBrands', 'CountryOfOrigin') IS NOT NULL ALTER TABLE dbo.TWBrands DROP COLUMN CountryOfOrigin;
IF COL_LENGTH('dbo.TWBrands', 'ParentCompany') IS NOT NULL   ALTER TABLE dbo.TWBrands DROP COLUMN ParentCompany;

COMMIT TRANSACTION;
GO

SET NOEXEC OFF;
GO
