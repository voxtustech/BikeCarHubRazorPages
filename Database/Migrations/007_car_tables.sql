/*
  007 - Car catalogue tables.
  PROPOSED - NOT APPLIED. Review before running. Run on Dev first, Production only after separate approval.
  Safe to run more than once: anything that already exists is skipped.

  What it does:
    - adds 3 empty columns to TWBrands (BrandSlug, CountryOfOrigin, ParentCompany)
    - creates 13 new empty tables and the view vw_FWKeySpecs
  What it does NOT do:
    - it adds no car data (the workbook import is a separate script)
    - it changes no existing row and no existing bike table apart from the 3 new TWBrands columns

  This script has no USE statement. It runs on whichever database is selected, so check the
  database name before pressing Execute. The first result shows the name it is running on.

  Deleting: no foreign key cascades. A model cannot be deleted while its variants, colours or
  images still exist, so one wrong DELETE cannot wipe a whole model.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Stop straight away if this is not a BikeCarHub database (for example, master is selected).
IF OBJECT_ID('dbo.TWBrands', 'U') IS NULL
BEGIN
    RAISERROR('STOPPED: TWBrands was not found. Select the BikeCarHub database and run again. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO

SELECT DB_NAME() AS RunningOnDatabase;
GO

-- All tables are created together: if any statement fails, everything in this block is undone.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Three new brand columns. Nullable, so existing brands are not affected.
IF COL_LENGTH('dbo.TWBrands', 'BrandSlug') IS NULL       ALTER TABLE dbo.TWBrands ADD BrandSlug nvarchar(120) NULL;
IF COL_LENGTH('dbo.TWBrands', 'CountryOfOrigin') IS NULL ALTER TABLE dbo.TWBrands ADD CountryOfOrigin nvarchar(100) NULL;
IF COL_LENGTH('dbo.TWBrands', 'ParentCompany') IS NULL   ALTER TABLE dbo.TWBrands ADD ParentCompany nvarchar(200) NULL;

-- 2. Separate bike and car text for brands that sell both (Honda, BMW).
IF OBJECT_ID('dbo.BrandProfiles', 'U') IS NULL
CREATE TABLE dbo.BrandProfiles (
    BrandProfileId   int IDENTITY(1,1) NOT NULL,
    TWBrandId        int NOT NULL,
    VehicleType      nvarchar(2) NOT NULL,
    IsPetrol         bit NULL,
    IsEV             bit NULL,
    Discription      nvarchar(max) NULL,
    MetaDescription  nvarchar(max) NULL,
    MetaKeywords     nvarchar(max) NULL,
    TitleTag         nvarchar(max) NULL,
    CreatedDateTime  datetime2 NULL CONSTRAINT DF_BrandProfiles_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_BrandProfiles PRIMARY KEY (BrandProfileId),
    CONSTRAINT FK_BrandProfiles_TWBrands FOREIGN KEY (TWBrandId) REFERENCES dbo.TWBrands (TWBrandId),
    CONSTRAINT CK_BrandProfiles_VehicleType CHECK (VehicleType IN ('TW', 'FW')),
    CONSTRAINT UQ_BrandProfiles_Brand_Type UNIQUE (TWBrandId, VehicleType)
);

-- 3. Car models.
IF OBJECT_ID('dbo.FourWheelers', 'U') IS NULL
CREATE TABLE dbo.FourWheelers (
    FourWheelerId    int IDENTITY(1,1) NOT NULL,
    FourWheelerName  nvarchar(200) NOT NULL,
    ModelSlug        nvarchar(200) NOT NULL,
    FWBrandId        int NOT NULL,
    BasePrice        int NULL,
    TopPrice         int NULL,
    [Type]           nvarchar(50) NULL,
    Segment          nvarchar(100) NULL,
    LaunchDate       datetime2 NULL,
    IsEV             bit NOT NULL CONSTRAINT DF_FourWheelers_IsEV DEFAULT 0,
    IsActive         bit NOT NULL CONSTRAINT DF_FourWheelers_IsActive DEFAULT 1,
    Discription      nvarchar(max) NULL,
    ImageFolder      nvarchar(400) NULL,
    NotesOrComments  nvarchar(max) NULL,
    CreatedDateTime  datetime2 NULL CONSTRAINT DF_FourWheelers_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_FourWheelers PRIMARY KEY (FourWheelerId),
    CONSTRAINT FK_FourWheelers_TWBrands FOREIGN KEY (FWBrandId) REFERENCES dbo.TWBrands (TWBrandId),
    CONSTRAINT UQ_FourWheelers_Brand_Slug UNIQUE (FWBrandId, ModelSlug)
);

-- 4. Variants of a model.
IF OBJECT_ID('dbo.FWVarients', 'U') IS NULL
CREATE TABLE dbo.FWVarients (
    FWVarientId      int IDENTITY(1,1) NOT NULL,
    FourWheelerId    int NOT NULL,
    Varients         nvarchar(200) NOT NULL,
    VariantName      nvarchar(100) NULL,
    VariantSlug      nvarchar(200) NULL,
    FuelType         nvarchar(30) NULL,
    Transmission     nvarchar(30) NULL,
    GearBox          nvarchar(20) NULL,
    ExShowroomPrice  int NULL,
    SeatingCapacity  int NULL,
    IsActive         bit NOT NULL CONSTRAINT DF_FWVarients_IsActive DEFAULT 1,
    CreatedDateTime  datetime2 NULL CONSTRAINT DF_FWVarients_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_FWVarients PRIMARY KEY (FWVarientId),
    CONSTRAINT FK_FWVarients_FourWheelers FOREIGN KEY (FourWheelerId) REFERENCES dbo.FourWheelers (FourWheelerId),
    CONSTRAINT UQ_FWVarients_Model_Name UNIQUE (FourWheelerId, Varients)
);

-- 5. Engine, mileage and performance. One row per variant.
IF OBJECT_ID('dbo.FWPowertrains', 'U') IS NULL
CREATE TABLE dbo.FWPowertrains (
    FWVarientId      int NOT NULL,
    EngineType       nvarchar(200) NULL,
    Displacement     int NULL,
    MaxPower         nvarchar(200) NULL,
    MaxTorque        nvarchar(200) NULL,
    PowerBhp         decimal(10,2) NULL,
    TorqueNm         decimal(10,2) NULL,
    NumOfCylinders   int NULL,
    ValvePerCylinder int NULL,
    Turbocharger     bit NULL,
    FuelSupply       nvarchar(50) NULL,
    DriveType        nvarchar(10) NULL,
    EmissionType     nvarchar(50) NULL,
    ARAIMileage      decimal(10,2) NULL,
    MileageUnit      nvarchar(10) NULL,
    FuelCapacity     decimal(10,2) NULL,
    CNGTankCapacity  decimal(10,2) NULL,
    TopSpeed         int NULL,
    ZeroTo100Kmph    decimal(10,2) NULL,
    CONSTRAINT PK_FWPowertrains PRIMARY KEY (FWVarientId),
    CONSTRAINT FK_FWPowertrains_FWVarients FOREIGN KEY (FWVarientId) REFERENCES dbo.FWVarients (FWVarientId)
);

-- 6. Battery, range and charging. One row per EV variant only.
IF OBJECT_ID('dbo.FWEVBatteries', 'U') IS NULL
CREATE TABLE dbo.FWEVBatteries (
    FWVarientId         int NOT NULL,
    BatteryCapacity     decimal(10,2) NULL,
    BatteryType         nvarchar(200) NULL,
    ClaimedRange        int NULL,
    RangeTestCycle      nvarchar(50) NULL,
    MotorType           nvarchar(200) NULL,
    ACChargingTime      nvarchar(200) NULL,
    DCChargingTime      nvarchar(200) NULL,
    DCChargingPower     decimal(10,2) NULL,
    BatteryWarranty     nvarchar(200) NULL,
    RegenerativeBraking nvarchar(50) NULL,
    CONSTRAINT PK_FWEVBatteries PRIMARY KEY (FWVarientId),
    CONSTRAINT FK_FWEVBatteries_FWVarients FOREIGN KEY (FWVarientId) REFERENCES dbo.FWVarients (FWVarientId)
);

-- 7. Size, suspension, steering, brakes, wheels. One row per variant.
IF OBJECT_ID('dbo.FWBodyAndChassis', 'U') IS NULL
CREATE TABLE dbo.FWBodyAndChassis (
    FWVarientId        int NOT NULL,
    [Length]           int NULL,
    Width              int NULL,
    Height             int NULL,
    Wheelbase          int NULL,
    GroundClearance    int NULL,
    KerbWeight         int NULL,
    BootSpace          int NULL,
    NumOfDoors         int NULL,
    NumOfSeatingRows   int NULL,
    TurningRadius      decimal(10,2) NULL,
    FrontSuspension    nvarchar(200) NULL,
    RearSuspension     nvarchar(200) NULL,
    SteeringType       nvarchar(200) NULL,
    SteeringAdjustment nvarchar(200) NULL,
    FrontBrake         nvarchar(200) NULL,
    RearBrake          nvarchar(200) NULL,
    WheelType          nvarchar(50) NULL,
    WheelSize          decimal(10,2) NULL,
    TyreSize           nvarchar(200) NULL,
    SpareWheel         nvarchar(50) NULL,
    CONSTRAINT PK_FWBodyAndChassis PRIMARY KEY (FWVarientId),
    CONSTRAINT FK_FWBodyAndChassis_FWVarients FOREIGN KEY (FWVarientId) REFERENCES dbo.FWVarients (FWVarientId)
);

-- 8. Safety equipment. One row per variant. Crash test ratings are in FWNcapRatings.
IF OBJECT_ID('dbo.FWSafety', 'U') IS NULL
CREATE TABLE dbo.FWSafety (
    FWVarientId                int NOT NULL,
    Airbags                    int NULL,
    [ABS]                      bit NULL,
    EBD                        bit NULL,
    ESC                        bit NULL,
    HillHoldAssist             bit NULL,
    HillDescentControl         bit NULL,
    TPMS                       bit NULL,
    RearParkingSensors         bit NULL,
    FrontParkingSensors        bit NULL,
    ParkingCamera              nvarchar(50) NULL,
    ISOFIX                     bit NULL,
    ADAS                       nvarchar(50) NULL,
    AllSeatThreePointSeatbelts bit NULL,
    RearDefogger               bit NULL,
    CONSTRAINT PK_FWSafety PRIMARY KEY (FWVarientId),
    CONSTRAINT FK_FWSafety_FWVarients FOREIGN KEY (FWVarientId) REFERENCES dbo.FWVarients (FWVarientId)
);

-- 9. Comfort, infotainment, exterior. One row per variant.
IF OBJECT_ID('dbo.FWFeatures', 'U') IS NULL
CREATE TABLE dbo.FWFeatures (
    FWVarientId                 int NOT NULL,
    AirConditioner              nvarchar(50) NULL,
    RearACVents                 bit NULL,
    PowerWindows                nvarchar(50) NULL,
    CruiseControl               bit NULL,
    KeylessEntry                bit NULL,
    PushButtonStart             bit NULL,
    Sunroof                     nvarchar(50) NULL,
    VentilatedSeats             nvarchar(50) NULL,
    PoweredDriverSeat           bit NULL,
    WirelessCharger             bit NULL,
    AutoHeadlamps               bit NULL,
    RainSensingWipers           bit NULL,
    ElectricFoldingORVM         bit NULL,
    AutoDimmingIRVM             bit NULL,
    RearWiperWasher             bit NULL,
    SeatUpholstery              nvarchar(50) NULL,
    AdditionalFeaturesOfVariant nvarchar(max) NULL,
    TouchscreenSize             nvarchar(50) NULL,
    AndroidAutoAppleCarPlay     nvarchar(50) NULL,
    Speakers                    int NULL,
    ConnectedCarTech            bit NULL,
    DigitalInstrumentCluster    nvarchar(50) NULL,
    HeadUpDisplay               bit NULL,
    AmbientLighting             bit NULL,
    Headlamps                   nvarchar(50) NULL,
    LEDDRLs                     bit NULL,
    FogLamps                    nvarchar(50) NULL,
    TailLamps                   nvarchar(50) NULL,
    ConnectedTailLamp           bit NULL,
    RoofRails                   bit NULL,
    DualToneRoof                bit NULL,
    CONSTRAINT PK_FWFeatures PRIMARY KEY (FWVarientId),
    CONSTRAINT FK_FWFeatures_FWVarients FOREIGN KEY (FWVarientId) REFERENCES dbo.FWVarients (FWVarientId)
);

-- 10. Crash test ratings per model. A model can later hold more than one test.
IF OBJECT_ID('dbo.FWNcapRatings', 'U') IS NULL
CREATE TABLE dbo.FWNcapRatings (
    FWNcapRatingId  int IDENTITY(1,1) NOT NULL,
    FourWheelerId   int NOT NULL,
    NCAPAgency      nvarchar(50) NULL,
    NCAPYear        int NULL,
    NCAPAdultRating int NULL,
    NCAPChildRating int NULL,
    NCAPAdultPoints decimal(10,2) NULL,
    NCAPChildPoints decimal(10,2) NULL,
    CONSTRAINT PK_FWNcapRatings PRIMARY KEY (FWNcapRatingId),
    CONSTRAINT FK_FWNcapRatings_FourWheelers FOREIGN KEY (FourWheelerId) REFERENCES dbo.FourWheelers (FourWheelerId)
);

-- 11. Colours of a model.
IF OBJECT_ID('dbo.FWColours', 'U') IS NULL
CREATE TABLE dbo.FWColours (
    FWColourId      int IDENTITY(1,1) NOT NULL,
    FourWheelerId   int NOT NULL,
    ColourName      nvarchar(200) NOT NULL,
    ColourSlug      nvarchar(200) NOT NULL,
    TopColorCode    nvarchar(20) NULL,
    BottomColorCode nvarchar(20) NULL,
    IsDualTone      bit NOT NULL CONSTRAINT DF_FWColours_IsDualTone DEFAULT 0,
    SortOrder       int NOT NULL CONSTRAINT DF_FWColours_SortOrder DEFAULT 0,
    ColourFolder    nvarchar(400) NULL,
    IsActive        bit NOT NULL CONSTRAINT DF_FWColours_IsActive DEFAULT 1,
    CreatedDateTime datetime2 NULL CONSTRAINT DF_FWColours_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_FWColours PRIMARY KEY (FWColourId),
    CONSTRAINT FK_FWColours_FourWheelers FOREIGN KEY (FourWheelerId) REFERENCES dbo.FourWheelers (FourWheelerId),
    CONSTRAINT UQ_FWColours_Model_Slug UNIQUE (FourWheelerId, ColourSlug)
);

-- 12. Which colours each variant comes in.
IF OBJECT_ID('dbo.FWVarientColours', 'U') IS NULL
CREATE TABLE dbo.FWVarientColours (
    FWVarientColourId int IDENTITY(1,1) NOT NULL,
    FWVarientId       int NOT NULL,
    FWColourId        int NOT NULL,
    SortOrder         int NOT NULL CONSTRAINT DF_FWVarientColours_SortOrder DEFAULT 0,
    IsAvailable       bit NOT NULL CONSTRAINT DF_FWVarientColours_IsAvailable DEFAULT 1,
    CONSTRAINT PK_FWVarientColours PRIMARY KEY (FWVarientColourId),
    CONSTRAINT FK_FWVarientColours_FWVarients FOREIGN KEY (FWVarientId) REFERENCES dbo.FWVarients (FWVarientId),
    CONSTRAINT FK_FWVarientColours_FWColours FOREIGN KEY (FWColourId) REFERENCES dbo.FWColours (FWColourId),
    CONSTRAINT UQ_FWVarientColours_Variant_Colour UNIQUE (FWVarientId, FWColourId)
);

-- 13. One row per image file. Stays empty until car photos are added.
IF OBJECT_ID('dbo.FWImages', 'U') IS NULL
CREATE TABLE dbo.FWImages (
    FWImageId       int IDENTITY(1,1) NOT NULL,
    FourWheelerId   int NOT NULL,
    FWColourId      int NULL,
    FWVarientId     int NULL,
    ImageType       nvarchar(20) NOT NULL,
    ViewAngle       nvarchar(20) NULL,
    SortOrder       int NOT NULL CONSTRAINT DF_FWImages_SortOrder DEFAULT 0,
    IsPrimary       bit NOT NULL CONSTRAINT DF_FWImages_IsPrimary DEFAULT 0,
    [FileName]      nvarchar(260) NULL,
    WebPPath        nvarchar(400) NOT NULL,
    ImageAltTag     nvarchar(400) NULL,
    Width           int NULL,
    Height          int NULL,
    [Source]        nvarchar(50) NULL,
    IsActive        bit NOT NULL CONSTRAINT DF_FWImages_IsActive DEFAULT 1,
    CreatedDateTime datetime2 NULL CONSTRAINT DF_FWImages_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_FWImages PRIMARY KEY (FWImageId),
    CONSTRAINT FK_FWImages_FourWheelers FOREIGN KEY (FourWheelerId) REFERENCES dbo.FourWheelers (FourWheelerId),
    CONSTRAINT FK_FWImages_FWColours FOREIGN KEY (FWColourId) REFERENCES dbo.FWColours (FWColourId),
    CONSTRAINT FK_FWImages_FWVarients FOREIGN KEY (FWVarientId) REFERENCES dbo.FWVarients (FWVarientId)
);

-- 14. Where each model's data came from.
IF OBJECT_ID('dbo.FWSources', 'U') IS NULL
CREATE TABLE dbo.FWSources (
    FWSourceId    int IDENTITY(1,1) NOT NULL,
    FourWheelerId int NOT NULL,
    SourceURLs    nvarchar(max) NULL,
    FetchedDate   datetime2 NULL,
    Notes         nvarchar(max) NULL,
    CONSTRAINT PK_FWSources PRIMARY KEY (FWSourceId),
    CONSTRAINT FK_FWSources_FourWheelers FOREIGN KEY (FourWheelerId) REFERENCES dbo.FourWheelers (FourWheelerId)
);

COMMIT TRANSACTION;
GO

-- If the block above failed, stop here so only the first error is shown.
IF COL_LENGTH('dbo.TWBrands', 'BrandSlug') IS NULL OR OBJECT_ID('dbo.FWSources', 'U') IS NULL
BEGIN
    RAISERROR('STOPPED: the tables were not created. Read the first error above. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO

-- 15. Indexes. A unique rule above already speeds up lookups on its first column.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TWBrands_BrandSlug' AND object_id = OBJECT_ID('dbo.TWBrands'))
    CREATE UNIQUE INDEX UX_TWBrands_BrandSlug ON dbo.TWBrands (BrandSlug) WHERE BrandSlug IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_FWVarients_Model_Slug' AND object_id = OBJECT_ID('dbo.FWVarients'))
    CREATE UNIQUE INDEX UX_FWVarients_Model_Slug ON dbo.FWVarients (FourWheelerId, VariantSlug) WHERE VariantSlug IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FourWheelers_Listing' AND object_id = OBJECT_ID('dbo.FourWheelers'))
    CREATE INDEX IX_FourWheelers_Listing ON dbo.FourWheelers (IsActive, FWBrandId, [Type], IsEV, BasePrice) INCLUDE (TopPrice, LaunchDate);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FWVarientColours_Colour' AND object_id = OBJECT_ID('dbo.FWVarientColours'))
    CREATE INDEX IX_FWVarientColours_Colour ON dbo.FWVarientColours (FWColourId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FWNcapRatings_Model' AND object_id = OBJECT_ID('dbo.FWNcapRatings'))
    CREATE INDEX IX_FWNcapRatings_Model ON dbo.FWNcapRatings (FourWheelerId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FWImages_Model' AND object_id = OBJECT_ID('dbo.FWImages'))
    CREATE INDEX IX_FWImages_Model ON dbo.FWImages (FourWheelerId, FWColourId, SortOrder);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FWImages_Colour' AND object_id = OBJECT_ID('dbo.FWImages'))
    CREATE INDEX IX_FWImages_Colour ON dbo.FWImages (FWColourId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FWImages_Variant' AND object_id = OBJECT_ID('dbo.FWImages'))
    CREATE INDEX IX_FWImages_Variant ON dbo.FWImages (FWVarientId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FWSources_Model' AND object_id = OBJECT_ID('dbo.FWSources'))
    CREATE INDEX IX_FWSources_Model ON dbo.FWSources (FourWheelerId);
GO

-- 16. The key-specs view. It replaces the workbook's FWSpec sheet, so these values are never stored twice.
--     If a model has several crash tests, the newest one is shown.
CREATE OR ALTER VIEW dbo.vw_FWKeySpecs
AS
SELECT
    v.FWVarientId,
    v.FourWheelerId,
    v.FuelType,
    p.ARAIMileage AS Mileage,
    p.MileageUnit,
    p.Displacement,
    p.MaxPower,
    p.MaxTorque,
    LTRIM(RTRIM(CONCAT(v.GearBox, ' ', v.Transmission))) AS Transmission,
    v.SeatingCapacity,
    b.BootSpace,
    b.GroundClearance,
    p.FuelCapacity,
    m.[Type] AS BodyType,
    s.Airbags,
    n.NCAPAdultRating AS NCAPRating,
    e.BatteryCapacity,
    e.ClaimedRange
FROM dbo.FWVarients v
JOIN dbo.FourWheelers m ON m.FourWheelerId = v.FourWheelerId
LEFT JOIN dbo.FWPowertrains p ON p.FWVarientId = v.FWVarientId
LEFT JOIN dbo.FWBodyAndChassis b ON b.FWVarientId = v.FWVarientId
LEFT JOIN dbo.FWSafety s ON s.FWVarientId = v.FWVarientId
LEFT JOIN dbo.FWEVBatteries e ON e.FWVarientId = v.FWVarientId
OUTER APPLY (
    SELECT TOP (1) r.NCAPAdultRating
    FROM dbo.FWNcapRatings r
    WHERE r.FourWheelerId = v.FourWheelerId
    ORDER BY r.NCAPYear DESC, r.FWNcapRatingId DESC
) n;
GO

-- 17. Check: expect 13 tables, 1 view and 3 new brand columns.
SELECT
    DB_NAME() AS RunningOnDatabase,
    (SELECT COUNT(*) FROM sys.tables WHERE name IN ('BrandProfiles', 'FourWheelers', 'FWVarients', 'FWPowertrains',
        'FWEVBatteries', 'FWBodyAndChassis', 'FWSafety', 'FWFeatures', 'FWNcapRatings', 'FWColours',
        'FWVarientColours', 'FWImages', 'FWSources')) AS NewTables_Expect13,
    (SELECT COUNT(*) FROM sys.views WHERE name = 'vw_FWKeySpecs') AS NewView_Expect1,
    (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TWBrands')
        AND name IN ('BrandSlug', 'CountryOfOrigin', 'ParentCompany')) AS NewBrandColumns_Expect3;
GO

SET NOEXEC OFF;
GO
