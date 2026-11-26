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

// Fuente CSV: encuestas internas.
public sealed class CsvSourceOptions
{
    public const string SectionName = "Extraction:Csv";

    // Ruta absoluta o relativa
    [Required]
    public string Folder { get; set; } = string.Empty;

    [Required]
    public string SearchPattern { get; set; } = "surveys*.csv";
}

// Fuente BD relacional: reseñas web.
public sealed class DatabaseSourceOptions
{
    public const string SectionName = "Extraction:Database";

    // Source_Type.Type de las reseñas web en ETL_Opinion.
    [Required]
    public string SourceType { get; set; } = string.Empty;

    [Range(1, 3600)]
    public int CommandTimeoutSeconds { get; set; } = 60;
}

// Fuente API REST: comentarios de redes sociales.
public sealed class ApiSourceOptions
{
    public const string SectionName = "Extraction:Api";

    [Required, Url]
    public string Url { get; set; } = string.Empty;

    // El máximo debe coincidir con SocialCommentsController.MaxPageSize.
    [Range(1, 500)]
    public int PageSize { get; set; } = 100;
}

public sealed class StagingOptions
{
    public const string SectionName = "Extraction:Staging";

    [Range(1, 100_000)]
    public int BatchSize { get; set; } = 1000;

    [Range(1, 3600)]
    public int CommandTimeoutSeconds { get; set; } = 120;
}

// Carga de dimensiones en OpinionAnalytics.
public sealed class LoadOptions
{
    public const string SectionName = "Load";

    [Range(1, 3600)]
    public int CommandTimeoutSeconds { get; set; } = 60;
}
