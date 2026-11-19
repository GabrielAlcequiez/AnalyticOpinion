-- =====================================================================
-- OpinionAnalytics - Área de Staging (Fase de Extracción)
-- Única tabla staging: recibe las opiniones crudas de las 3 fuentes
-- (CSV de encuestas, BD relacional de reseñas web, API de redes sociales).
-- Los datos se guardan como texto, sin transformar; la validación y
-- conversión de tipos ocurre en la fase de Transformación.
-- Los maestros (cliente, producto, fuente, clasificación) NO pasan por
-- staging: la fase de Carga los lee directo de ETL_Opinion.
-- =====================================================================

USE OpinionAnalytics;
GO

IF OBJECT_ID('dbo.StageOpinion', 'U') IS NOT NULL
    DROP TABLE dbo.StageOpinion;
GO

CREATE TABLE dbo.StageOpinion (
    ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    BatchId UNIQUEIDENTIFIER NOT NULL,

    -- 'Survey' | 'WebReview' | 'SocialComment'
    Origin VARCHAR(30) NOT NULL,

    -- IDs de la fuente original (trazabilidad)
    OriginalOpinionID VARCHAR(30) NOT NULL,
    OriginalClientID VARCHAR(30) NULL,
    OriginalProductID VARCHAR(30) NULL,

    Comment NVARCHAR(MAX) NULL,
    OriginalDate VARCHAR(30) NULL,
    OriginalClassification NVARCHAR(30) NULL,
    OriginalScore VARCHAR(20) NULL,

    -- EncuestaInterna / Reseña Web / Twitter / Instagram / Facebook
    SourceName NVARCHAR(50) NULL,
    -- Data_Source.ID en ETL_Opinion (para mapear DimSource.DataSourceID)
    OriginalDataSourceID VARCHAR(30) NULL,

    ExtractedAt DATETIME2(3) NOT NULL
        CONSTRAINT DF_StageOpinion_ExtractedAt DEFAULT SYSUTCDATETIME()
) ON [PRIMARY];
GO

CREATE INDEX IX_StageOpinion_Origin
ON dbo.StageOpinion (Origin, OriginalOpinionID);
GO
