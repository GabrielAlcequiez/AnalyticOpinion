namespace AnalyticOpinion.Data.Persistence;

internal static class SourceQueries
{
    private const string OpinionColumns = """
        SELECT
            o.ID                 AS OpinionId,
            o.Client_ID          AS ClientId,
            o.Product_ID         AS ProductId,
            o.Comment            AS Comment,
            o.[Date]             AS OpinionDate,
            cl.Type              AS Classification,
            o.Satisfaction_Score AS Score,
            ds.Name              AS SourceName,
            ds.ID                AS DataSourceId
        """;

    private const string OpinionFrom = """
        FROM dbo.Opinion o
        INNER JOIN dbo.Data_Source ds    ON ds.ID = o.Data_Source_ID
        INNER JOIN dbo.Source_Type st    ON st.ID = ds.Source_Type_ID
        INNER JOIN dbo.Classification cl ON cl.ID = o.Classification_ID
        WHERE st.Type = @SourceType
        """;

    public const string OpinionsBySourceType = OpinionColumns + "\n" + OpinionFrom + "\nORDER BY o.ID;";

    // Devuelve dos result sets: el total y la página solicitada.
    public const string PagedOpinionsBySourceType =
        "SELECT COUNT(*)\n" + OpinionFrom + ";\n" +
        OpinionColumns + "\n" + OpinionFrom + "\nORDER BY o.ID\nOFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
}
