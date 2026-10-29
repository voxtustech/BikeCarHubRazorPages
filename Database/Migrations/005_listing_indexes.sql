/*
  005 - Indexes for listing, search and sort.
  PROPOSED - NOT APPLIED. Review before running. Safe to run more than once.

  With 355 models the site filters the catalogue in memory, so these are not needed today.
  They matter once listing queries move to SQL (cars, used vehicles, thousands of rows).
  TwoWheelerName and Price are nvarchar(max) and cannot be index keys; BasePrice is used for price.
*/
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TwoWheelers_Listing')
    CREATE INDEX IX_TwoWheelers_Listing
        ON TwoWheelers (IsActive, TwoWBrandId, Type, IsEV, BasePrice)
        INCLUDE (TopPrice, CreatedDateTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TwoWheelers_Price')
    CREATE INDEX IX_TwoWheelers_Price ON TwoWheelers (BasePrice) WHERE IsActive = 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Blogs_Date')
    CREATE INDEX IX_Blogs_Date ON Blogs ([Date] DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LatestNews_Date')
    CREATE INDEX IX_LatestNews_Date ON LatestNews ([Date] DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ValueForMoney_Date')
    CREATE INDEX IX_ValueForMoney_Date ON ValueForMoney ([Date] DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TWUpcomingBikes_FilterLaunchDate')
    CREATE INDEX IX_TWUpcomingBikes_FilterLaunchDate ON TWUpcomingBikes (FilterLaunchDate);
GO
