using System.ComponentModel.DataAnnotations;

namespace AnalyticOpinion.Data.Configuration;

public static class ConnectionNames
{
    public const string SourceDb = "SourceDb";
    public const string AnalyticsDb = "AnalyticsDb";
}

public sealed class SocialCommentsOptions
{
    public const string SectionName = "SocialComments";

    // Source_Type.Type de las redes sociales en ETL_Opinion.
    [Required]
    public string SourceType { get; set; } = "Red Social";

    [Range(1, 3600)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}
