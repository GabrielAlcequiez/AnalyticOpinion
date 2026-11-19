using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Configuration;
using AnalyticOpinion.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AnalyticOpinion.Data.Persistence;

public sealed class SocialCommentRepository(
    ISqlConnectionFactory connectionFactory,
    IOptions<SocialCommentsOptions> options) : ISocialCommentRepository
{
    private readonly SocialCommentsOptions _options = options.Value;

    public async Task<PagedResult<SocialCommentDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        await using var connection = connectionFactory.CreateSourceConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(SourceQueries.PagedOpinionsBySourceType, connection)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };
        command.Parameters.Add("@SourceType", System.Data.SqlDbType.NVarChar, 50).Value = _options.SourceType;
        command.Parameters.Add("@Offset", System.Data.SqlDbType.Int).Value = (page - 1) * pageSize;
        command.Parameters.Add("@PageSize", System.Data.SqlDbType.Int).Value = pageSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);
        var totalCount = reader.GetInt32(0);

        await reader.NextResultAsync(cancellationToken);
        var items = new List<SocialCommentDto>(pageSize);
        var ordinals = OpinionOrdinals.From(reader);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SocialCommentDto(
                Id: reader.GetInt32(ordinals.OpinionId),
                ClientId: reader.IsDBNull(ordinals.ClientId) ? null : reader.GetInt32(ordinals.ClientId),
                ProductId: reader.IsDBNull(ordinals.ProductId) ? null : reader.GetInt32(ordinals.ProductId),
                Comment: reader.GetString(ordinals.Comment),
                Date: DateOnly.FromDateTime(reader.GetDateTime(ordinals.OpinionDate)),
                Classification: reader.GetString(ordinals.Classification),
                Score: reader.IsDBNull(ordinals.Score) ? null : reader.GetInt32(ordinals.Score),
                Platform: reader.GetString(ordinals.SourceName),
                DataSourceId: reader.GetInt32(ordinals.DataSourceId)));
        }

        return new PagedResult<SocialCommentDto>(items, page, pageSize, totalCount);
    }
}

internal readonly record struct OpinionOrdinals(
    int OpinionId, int ClientId, int ProductId, int Comment, int OpinionDate,
    int Classification, int Score, int SourceName, int DataSourceId)
{
    public static OpinionOrdinals From(SqlDataReader reader) => new(
        reader.GetOrdinal("OpinionId"),
        reader.GetOrdinal("ClientId"),
        reader.GetOrdinal("ProductId"),
        reader.GetOrdinal("Comment"),
        reader.GetOrdinal("OpinionDate"),
        reader.GetOrdinal("Classification"),
        reader.GetOrdinal("Score"),
        reader.GetOrdinal("SourceName"),
        reader.GetOrdinal("DataSourceId"));
}
