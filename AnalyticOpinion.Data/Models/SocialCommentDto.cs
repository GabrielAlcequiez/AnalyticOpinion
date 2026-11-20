namespace AnalyticOpinion.Data.Models;

public sealed record SocialCommentDto(
    int Id,
    int? ClientId,
    int? ProductId,
    string Comment,
    DateOnly Date,
    string Classification,
    int? Score,
    string Platform,
    int DataSourceId);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
