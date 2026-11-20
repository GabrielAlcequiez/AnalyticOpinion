namespace AnalyticOpinion.Data.Models;

public sealed record ExtractionResult(
    string Source,
    int RowsExtracted,
    int RowsStaged,
    TimeSpan Elapsed,
    string? Error = null)
{
    public bool Succeeded => Error is null;
}

public sealed record ExtractionRunSummary(
    Guid BatchId,
    IReadOnlyList<ExtractionResult> Results,
    TimeSpan TotalElapsed)
{
    public bool AllSucceeded => Results.All(r => r.Succeeded);
    public int TotalRowsStaged => Results.Sum(r => r.RowsStaged);
}
