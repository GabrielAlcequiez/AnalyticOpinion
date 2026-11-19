
USE OpinionAnalytics;
GO

-- DIMENSIONES: FG_DIMENSIONS

CREATE TABLE dbo.DimDate (
    DateID INT PRIMARY KEY NOT NULL,
    FullDate DATE UNIQUE NOT NULL,
    YearNumber SMALLINT NOT NULL,
    QuarterNumber TINYINT NOT NULL,
    MonthNumber TINYINT NOT NULL,
    DayNumber TINYINT NOT NULL
) ON FG_DIMENSIONS;
GO

CREATE TABLE dbo.DimProduct (
    ProductID INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
    OriginalProductID INT UNIQUE NOT NULL,
    ProductName NVARCHAR(75) NOT NULL,
    CategoryName NVARCHAR(50) NOT NULL
) ON FG_DIMENSIONS;
GO

CREATE TABLE dbo.DimClient (
    ClientID INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
    OriginalClientID INT UNIQUE NOT NULL,
    ClientName NVARCHAR(50) NOT NULL,
    ClientEmail VARCHAR(100) NOT NULL
) ON FG_DIMENSIONS;
GO

CREATE TABLE dbo.DimSource (
    SourceID INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
    DataSourceID INT UNIQUE NOT NULL,
    SourceName NVARCHAR(50) NOT NULL,
    SourceType NVARCHAR(50) NOT NULL,
    ExternalID VARCHAR(30) NULL,
    SourceLoadDate DATE NOT NULL
) ON FG_DIMENSIONS;
GO

CREATE TABLE dbo.DimClassification (
    ClassificationID INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
    OriginalClassificationID INT UNIQUE NOT NULL,
    ClassificationType NVARCHAR(50) NOT NULL
) ON FG_DIMENSIONS;
GO

-- TABLA DE HECHOS: FG_FACTS

CREATE TABLE dbo.FactOpinion (
    OpinionID INT PRIMARY KEY NOT NULL,
    StageOpinionID INT UNIQUE NOT NULL,
    Comment NVARCHAR(MAX) NOT NULL,
    SatisfactionScore INT NULL,
    DateID INT NOT NULL,
    ClientID INT NULL,
    ProductID INT NULL,
    SourceID INT NOT NULL,
    ClassificationID INT NOT NULL
) ON FG_FACTS;
GO


-- FOREIGN KEYS


ALTER TABLE dbo.FactOpinion
ADD CONSTRAINT FK_FactOpinion_DimDate
FOREIGN KEY (DateID)
REFERENCES dbo.DimDate(DateID);
GO

ALTER TABLE dbo.FactOpinion
ADD CONSTRAINT FK_FactOpinion_DimClient
FOREIGN KEY (ClientID)
REFERENCES dbo.DimClient(ClientID);
GO

ALTER TABLE dbo.FactOpinion
ADD CONSTRAINT FK_FactOpinion_DimProduct
FOREIGN KEY (ProductID)
REFERENCES dbo.DimProduct(ProductID);
GO

ALTER TABLE dbo.FactOpinion
ADD CONSTRAINT FK_FactOpinion_DimSource
FOREIGN KEY (SourceID)
REFERENCES dbo.DimSource(SourceID);
GO

ALTER TABLE dbo.FactOpinion
ADD CONSTRAINT FK_FactOpinion_DimClassification
FOREIGN KEY (ClassificationID)
REFERENCES dbo.DimClassification(ClassificationID);
GO


