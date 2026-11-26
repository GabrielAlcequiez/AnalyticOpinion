namespace AnalyticOpinion.Data.Persistence;

// Consultas a los maestros de ETL_Opinion (SourceDb).
// El orden y los alias de las columnas deben coincidir con los tipos de tabla
// de OpinionAnalytics_LoadDimensions.sql: el TVP se asocia por posición.
internal static class MasterQueries
{
    public const string Clients = """
        SELECT c.ID    AS OriginalClientID,
               c.Name  AS ClientName,
               c.Email AS ClientEmail
        FROM dbo.Client c;
        """;

    public const string Products = """
        SELECT p.ID   AS OriginalProductID,
               p.Name AS ProductName,
               c.Name AS CategoryName
        FROM dbo.Product p
        INNER JOIN dbo.Category c ON c.ID = p.Category_ID;
        """;

    public const string Sources = """
        SELECT ds.ID          AS DataSourceID,
               ds.Name        AS SourceName,
               st.Type        AS SourceType,
               ds.External_ID AS ExternalID,
               ds.Load_Date   AS SourceLoadDate
        FROM dbo.Data_Source ds
        INNER JOIN dbo.Source_Type st ON st.ID = ds.Source_Type_ID;
        """;

    public const string Classifications = """
        SELECT cl.ID   AS OriginalClassificationID,
               cl.Type AS ClassificationType
        FROM dbo.Classification cl;
        """;
}
