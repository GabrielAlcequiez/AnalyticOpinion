namespace AnalyticOpinion.Data.Models;

// Todo se guarda como texto, tal como viene de la fuente; los tipos se validan en la Transformación.
public sealed record StageOpinion
{
    public required Guid BatchId { get; init; }
    public required string Origin { get; init; }
    public required string OriginalOpinionId { get; init; }
    public string? OriginalClientId { get; init; }
    public string? OriginalProductId { get; init; }
    public string? Comment { get; init; }
    public string? OriginalDate { get; init; }
    public string? OriginalClassification { get; init; }
    public string? OriginalScore { get; init; }
    public string? SourceName { get; init; }
    public string? OriginalDataSourceId { get; init; }
}

public static class OpinionOrigin
{
    public const string Survey = "Survey";
    public const string WebReview = "WebReview";
    public const string SocialComment = "SocialComment";
}

// Deben coincidir con las columnas de OpinionAnalytics_Staging.sql.
public static class StageOpinionLimits
{
    public const int OriginalId = 30;
    public const int OriginalDate = 30;
    public const int OriginalClassification = 30;
    public const int OriginalScore = 20;
    public const int SourceName = 50;
}
