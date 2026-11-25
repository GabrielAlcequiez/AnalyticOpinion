using System.Data;
using AnalyticOpinion.Data.Configuration;
using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyticOpinion.Data.Persistence;

// Cada fuente se inserta en su propia transacción: entra completa o no entra.
public sealed class StagingRepository(
    ISqlConnectionFactory connectionFactory,
    IOptions<StagingOptions> options,
    ILogger<StagingRepository> logger) : IStagingRepository
{
    // Fijo a propósito: es parte del esquema (OpinionAnalytics_Staging.sql) y se usa en SQL dinámico,
    // así que no se expone en configuración.
    private const string TableName = "dbo.StageOpinion";

    private readonly StagingOptions _options = options.Value;

    public async Task TruncateAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateAnalyticsConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand($"TRUNCATE TABLE {TableName};", connection)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };
        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Tabla {Table} vaciada", TableName);
    }

    public async Task<int> BulkInsertAsync(IReadOnlyCollection<StageOpinion> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return 0;
        }

        using var table = BuildDataTable(rows);

        await using var connection = connectionFactory.CreateAnalyticsConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints, transaction)
            {
                DestinationTableName = TableName,
                BatchSize = _options.BatchSize,
                BulkCopyTimeout = _options.CommandTimeoutSeconds,
                EnableStreaming = true
            };

            foreach (DataColumn column in table.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(table, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return rows.Count;
    }

    private static DataTable BuildDataTable(IReadOnlyCollection<StageOpinion> rows)
    {
        // ID y ExtractedAt los genera SQL Server.
        var table = new DataTable();
        table.Columns.Add("BatchId", typeof(Guid));
        table.Columns.Add("Origin", typeof(string));
        table.Columns.Add("OriginalOpinionID", typeof(string));
        table.Columns.Add("OriginalClientID", typeof(string));
        table.Columns.Add("OriginalProductID", typeof(string));
        table.Columns.Add("Comment", typeof(string));
        table.Columns.Add("OriginalDate", typeof(string));
        table.Columns.Add("OriginalClassification", typeof(string));
        table.Columns.Add("OriginalScore", typeof(string));
        table.Columns.Add("SourceName", typeof(string));
        table.Columns.Add("OriginalDataSourceID", typeof(string));

        foreach (var row in rows)
        {
            table.Rows.Add(
                row.BatchId,
                row.Origin,
                row.OriginalOpinionId,
                (object?)row.OriginalClientId ?? DBNull.Value,
                (object?)row.OriginalProductId ?? DBNull.Value,
                (object?)row.Comment ?? DBNull.Value,
                (object?)row.OriginalDate ?? DBNull.Value,
                (object?)row.OriginalClassification ?? DBNull.Value,
                (object?)row.OriginalScore ?? DBNull.Value,
                (object?)row.SourceName ?? DBNull.Value,
                (object?)row.OriginalDataSourceId ?? DBNull.Value);
        }

        return table;
    }
}
