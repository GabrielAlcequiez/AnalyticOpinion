using System.Data;
using System.Diagnostics;
using AnalyticOpinion.Data.Configuration;
using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyticOpinion.Data.Loaders;

// Lee cada maestro de ETL_Opinion y lo envía completo, en una sola llamada, como
// Table-Valued Parameter al procedure de OpinionAnalytics, que hace el upsert en
// una transacción (ver OpinionAnalytics_LoadDimensions.sql).
public sealed class DimensionLoader(
    ISqlConnectionFactory connectionFactory,
    IOptions<LoadOptions> options,
    ILogger<DimensionLoader> logger) : IDimensionLoader
{
    private readonly LoadOptions _options = options.Value;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var total = Stopwatch.StartNew();
        logger.LogInformation("Iniciando carga de dimensiones");

        await LoadFromMasterAsync("DimClient", MasterQueries.Clients,
            "dbo.usp_LoadDimClient", "dbo.ClientTableType", cancellationToken);
        await LoadFromMasterAsync("DimProduct", MasterQueries.Products,
            "dbo.usp_LoadDimProduct", "dbo.ProductTableType", cancellationToken);
        await LoadFromMasterAsync("DimSource", MasterQueries.Sources,
            "dbo.usp_LoadDimSource", "dbo.SourceTableType", cancellationToken);
        await LoadFromMasterAsync("DimClassification", MasterQueries.Classifications,
            "dbo.usp_LoadDimClassification", "dbo.ClassificationTableType", cancellationToken);
        await LoadDateAsync(cancellationToken);

        logger.LogInformation("Carga de dimensiones finalizada en {ElapsedMs} ms", total.ElapsedMilliseconds);
    }

    private async Task LoadFromMasterAsync(
        string dimension, string sourceQuery, string procedure, string tableType, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var rows = await ReadMasterAsync(sourceQuery, cancellationToken);

        await using var connection = connectionFactory.CreateAnalyticsConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateProcedureCommand(procedure, connection);

        command.Parameters.Add(new SqlParameter("@Rows", SqlDbType.Structured) { TypeName = tableType, Value = rows });
        var inserted = AddOutputParameter(command, "@Inserted");
        var updated = AddOutputParameter(command, "@Updated");

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation(
            "[{Dimension}] {Read} filas leídas del maestro: {Inserted} insertadas, {Updated} actualizadas en {ElapsedMs} ms",
            dimension, rows.Rows.Count, inserted.Value, updated.Value, stopwatch.ElapsedMilliseconds);
    }

    // DimDate no tiene maestro: el procedure genera las fechas a partir de StageOpinion.
    private async Task LoadDateAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        await using var connection = connectionFactory.CreateAnalyticsConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateProcedureCommand("dbo.usp_LoadDimDate", connection);
        var inserted = AddOutputParameter(command, "@Inserted");

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("[DimDate] {Inserted} fechas insertadas en {ElapsedMs} ms",
            inserted.Value, stopwatch.ElapsedMilliseconds);
    }

    private async Task<DataTable> ReadMasterAsync(string query, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateSourceConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(query, connection) { CommandTimeout = _options.CommandTimeoutSeconds };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private SqlCommand CreateProcedureCommand(string procedure, SqlConnection connection) =>
        new(procedure, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _options.CommandTimeoutSeconds
        };

    private static SqlParameter AddOutputParameter(SqlCommand command, string name) =>
        command.Parameters.Add(new SqlParameter(name, SqlDbType.Int) { Direction = ParameterDirection.Output });
}
