USE OpinionAnalytics;
GO

-- Solo para idempotencia y poder volver a correrlo completo, sin problema alguno
DROP PROCEDURE IF EXISTS dbo.usp_LoadDimClient;
DROP PROCEDURE IF EXISTS dbo.usp_LoadDimProduct;
DROP PROCEDURE IF EXISTS dbo.usp_LoadDimSource;
DROP PROCEDURE IF EXISTS dbo.usp_LoadDimClassification;
DROP PROCEDURE IF EXISTS dbo.usp_LoadDimDate;
GO

DROP TYPE IF EXISTS dbo.ClientTableType;
DROP TYPE IF EXISTS dbo.ProductTableType;
DROP TYPE IF EXISTS dbo.SourceTableType;
DROP TYPE IF EXISTS dbo.ClassificationTableType;
GO

-- Se utilizará una metodología SCD de tipo 1, osea actualiza todas las filas e ingresa las que no existen
-- todos los métodos son upsert

-- TVP

CREATE TYPE dbo.ClientTableType AS TABLE (
    OriginalClientID INT           NOT NULL PRIMARY KEY,
    ClientName       NVARCHAR(50)  NOT NULL,
    ClientEmail      VARCHAR(100)  NOT NULL
);
GO

CREATE TYPE dbo.ProductTableType AS TABLE (
    OriginalProductID INT          NOT NULL PRIMARY KEY,
    ProductName       NVARCHAR(75) NOT NULL,
    CategoryName      NVARCHAR(50) NOT NULL
);
GO

CREATE TYPE dbo.SourceTableType AS TABLE (
    DataSourceID   INT          NOT NULL PRIMARY KEY,
    SourceName     NVARCHAR(50) NOT NULL,
    SourceType     NVARCHAR(50) NOT NULL,
    ExternalID     VARCHAR(30)  NULL,
    SourceLoadDate DATE         NOT NULL
);
GO

CREATE TYPE dbo.ClassificationTableType AS TABLE (
    OriginalClassificationID INT          NOT NULL PRIMARY KEY,
    ClassificationType       NVARCHAR(50) NOT NULL
);
GO

-- Procedures 

CREATE PROCEDURE dbo.usp_LoadDimClient
    @Rows     dbo.ClientTableType READONLY,
    @Inserted INT OUTPUT,
    @Updated  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE d
    SET d.ClientName  = r.ClientName,
        d.ClientEmail = r.ClientEmail
    FROM dbo.DimClient AS d
    INNER JOIN @Rows AS r ON r.OriginalClientID = d.OriginalClientID
    WHERE EXISTS (SELECT r.ClientName, r.ClientEmail
                  EXCEPT
                  SELECT d.ClientName, d.ClientEmail);
    SET @Updated = @@ROWCOUNT;

    INSERT INTO dbo.DimClient (OriginalClientID, ClientName, ClientEmail)
    SELECT r.OriginalClientID, r.ClientName, r.ClientEmail
    FROM @Rows AS r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DimClient AS d
                      WHERE d.OriginalClientID = r.OriginalClientID);
    SET @Inserted = @@ROWCOUNT;

    COMMIT TRANSACTION;
END;
GO

CREATE PROCEDURE dbo.usp_LoadDimProduct
    @Rows     dbo.ProductTableType READONLY,
    @Inserted INT OUTPUT,
    @Updated  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE d
    SET d.ProductName  = r.ProductName,
        d.CategoryName = r.CategoryName
    FROM dbo.DimProduct AS d
    INNER JOIN @Rows AS r ON r.OriginalProductID = d.OriginalProductID
    WHERE EXISTS (SELECT r.ProductName, r.CategoryName
                  EXCEPT
                  SELECT d.ProductName, d.CategoryName);
    SET @Updated = @@ROWCOUNT;

    INSERT INTO dbo.DimProduct (OriginalProductID, ProductName, CategoryName)
    SELECT r.OriginalProductID, r.ProductName, r.CategoryName
    FROM @Rows AS r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DimProduct AS d
                      WHERE d.OriginalProductID = r.OriginalProductID);
    SET @Inserted = @@ROWCOUNT;

    COMMIT TRANSACTION;
END;
GO

CREATE PROCEDURE dbo.usp_LoadDimSource
    @Rows     dbo.SourceTableType READONLY,
    @Inserted INT OUTPUT,
    @Updated  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE d
    SET d.SourceName     = r.SourceName,
        d.SourceType     = r.SourceType,
        d.ExternalID     = r.ExternalID,
        d.SourceLoadDate = r.SourceLoadDate
    FROM dbo.DimSource AS d
    INNER JOIN @Rows AS r ON r.DataSourceID = d.DataSourceID
    WHERE EXISTS (SELECT r.SourceName, r.SourceType, r.ExternalID, r.SourceLoadDate
                  EXCEPT
                  SELECT d.SourceName, d.SourceType, d.ExternalID, d.SourceLoadDate);
    SET @Updated = @@ROWCOUNT;

    INSERT INTO dbo.DimSource (DataSourceID, SourceName, SourceType, ExternalID, SourceLoadDate)
    SELECT r.DataSourceID, r.SourceName, r.SourceType, r.ExternalID, r.SourceLoadDate
    FROM @Rows AS r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DimSource AS d
                      WHERE d.DataSourceID = r.DataSourceID);
    SET @Inserted = @@ROWCOUNT;

    COMMIT TRANSACTION;
END;
GO

CREATE PROCEDURE dbo.usp_LoadDimClassification
    @Rows     dbo.ClassificationTableType READONLY,
    @Inserted INT OUTPUT,
    @Updated  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE d
    SET d.ClassificationType = r.ClassificationType
    FROM dbo.DimClassification AS d
    INNER JOIN @Rows AS r ON r.OriginalClassificationID = d.OriginalClassificationID
    WHERE d.ClassificationType <> r.ClassificationType; -- columna NOT NULL: basta con "<>"
    SET @Updated = @@ROWCOUNT;

    INSERT INTO dbo.DimClassification (OriginalClassificationID, ClassificationType)
    SELECT r.OriginalClassificationID, r.ClassificationType
    FROM @Rows AS r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DimClassification AS d
                      WHERE d.OriginalClassificationID = r.OriginalClassificationID);
    SET @Inserted = @@ROWCOUNT;

    COMMIT TRANSACTION;
END;
GO


CREATE PROCEDURE dbo.usp_LoadDimDate
    @Inserted INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Inserta las fechas distintas de StageOpinion que aún no están en DimDate.
    INSERT INTO dbo.DimDate (DateID, FullDate, YearNumber, QuarterNumber, MonthNumber, DayNumber)
    SELECT YEAR(d.FullDate) * 10000 + MONTH(d.FullDate) * 100 + DAY(d.FullDate), -- ej. 20250715
           d.FullDate,
           YEAR(d.FullDate),
           DATEPART(QUARTER, d.FullDate),
           MONTH(d.FullDate),
           DAY(d.FullDate)
    FROM (SELECT DISTINCT TRY_CONVERT(DATE, OriginalDate, 23) AS FullDate -- NULL si la fecha no es válida
          FROM dbo.StageOpinion) AS d
    WHERE d.FullDate IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.DimDate AS x WHERE x.FullDate = d.FullDate);

    SET @Inserted = @@ROWCOUNT;
END;
GO

