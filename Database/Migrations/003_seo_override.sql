/*
  003 - SeoOverride: optional hand-written title / description / H1 for editorial pages.
  PROPOSED - NOT APPLIED. Review before running. Safe to run more than once.

  Titles and descriptions are generated from templates in code. A row here replaces the
  generated text for one page; generated text is never stored.
*/
IF OBJECT_ID('SeoOverride', 'U') IS NULL
BEGIN
    CREATE TABLE SeoOverride
    (
        SeoOverrideId   int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeoOverride PRIMARY KEY,
        Path            varchar(400)   NOT NULL,
        Title           nvarchar(200)  NULL,
        MetaDescription nvarchar(400)  NULL,
        H1              nvarchar(200)  NULL,
        NoIndex         bit            NOT NULL CONSTRAINT DF_SeoOverride_NoIndex DEFAULT 0,
        UpdatedDateTime datetime2      NOT NULL CONSTRAINT DF_SeoOverride_Updated DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_SeoOverride_Path ON SeoOverride (Path);
END
GO
