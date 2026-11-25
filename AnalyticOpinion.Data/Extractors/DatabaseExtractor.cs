using System.Globalization;
using AnalyticOpinion.Data.Configuration;
using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Models;
using AnalyticOpinion.Data.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyticOpinion.Data.Extractors;

public sealed class DatabaseExtractor(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseSourceOptions> options,
    ILogger<DatabaseExtractor> logger) : IExtractor
{
    private readonly DatabaseSourceOptions _options = options.Value;

    public string Name => "BD relacional - Reseñas web";

    public async Task<IReadOnlyList<StageOpinion>> ExtractAsync(Guid batchId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateSourceConnection();
        await connection.OpenAsync(cancellationToken);
        logger.LogDebug("Conectado a {Database} en {Server}", connection.Database, connection.DataSource);

        await using var command = new SqlCommand(SourceQueries.OpinionsBySourceType, connection)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };
        command.Parameters.Add("@SourceType", System.Data.SqlDbType.NVarChar, 50).Value = _options.SourceType;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var o = OpinionOrdinals.From(reader);
        var rows = new List<StageOpinion>();

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new StageOpinion
            {
                BatchId = batchId,
                Origin = OpinionOrigin.WebReview,
                OriginalOpinionId = ToText(reader.GetInt32(o.OpinionId)),
                OriginalClientId = reader.IsDBNull(o.ClientId) ? null : ToText(reader.GetInt32(o.ClientId)),
                OriginalProductId = reader.IsDBNull(o.ProductId) ? null : ToText(reader.GetInt32(o.ProductId)),
                Comment = reader.IsDBNull(o.Comment) ? null : reader.GetString(o.Comment),
                OriginalDate = reader.GetDateTime(o.OpinionDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                OriginalClassification = reader.GetString(o.Classification),
                OriginalScore = reader.IsDBNull(o.Score) ? null : ToText(reader.GetInt32(o.Score)),
                SourceName = reader.GetString(o.SourceName),
                OriginalDataSourceId = ToText(reader.GetInt32(o.DataSourceId))
            });
        }

        if (rows.Count == 0)
        {
            logger.LogWarning("La consulta no devolvió reseñas para Source_Type = '{SourceType}'", _options.SourceType);
        }

        return rows;
    }

    private static string ToText(int value) => value.ToString(CultureInfo.InvariantCulture);
}
