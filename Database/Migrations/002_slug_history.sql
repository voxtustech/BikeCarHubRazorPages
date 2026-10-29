/*
  002 - SlugHistory: old address -> current record, for 301 redirects after a rename.
  PROPOSED - NOT APPLIED. Review before running. Safe to run more than once.

  EntityType: 'brand', 'model', 'variant', 'blog', 'news', 'value-for-money', 'upcoming'.
  OldPath is the full old path, lowercase, e.g. '/new-bikes/bajaj/pulsar-150'.
*/
IF OBJECT_ID('SlugHistory', 'U') IS NULL
BEGIN
    CREATE TABLE SlugHistory
    (
        SlugHistoryId   int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SlugHistory PRIMARY KEY,
        EntityType      varchar(30)   NOT NULL,
        EntityId        int           NOT NULL,
        OldPath         varchar(400)  NOT NULL,
        CreatedDateTime datetime2     NOT NULL CONSTRAINT DF_SlugHistory_Created DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_SlugHistory_OldPath ON SlugHistory (OldPath);
    CREATE INDEX IX_SlugHistory_Entity ON SlugHistory (EntityType, EntityId);
END
GO
