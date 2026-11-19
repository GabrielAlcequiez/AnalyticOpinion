using System.Globalization;
using System.Text;
using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Extractors.Csv;
using AnalyticOpinion.Data.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnalyticOpinion.Data.Extractors;

public sealed class CsvExtractor(
    IHostEnvironment environment,
    ILogger<CsvExtractor> logger) : IExtractor
{
    // Carpeta "data" en la raíz de la solución (relativa al proyecto Worker).
    private const string CsvFolder = @"..\data";
    private const string SearchPattern = "surveys*.csv";

    public string Name => "CSV - Encuestas internas";

    public async Task<IReadOnlyList<StageOpinion>> ExtractAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var directory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, CsvFolder));
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"No existe la carpeta de CSV: {directory}");
        }

        var files = Directory.GetFiles(directory, SearchPattern).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Length == 0)
        {
            logger.LogWarning("No se encontraron archivos '{Pattern}' en {Directory}", SearchPattern, directory);
            return [];
        }

        var rows = new List<StageOpinion>();
        foreach (var file in files)
        {
            rows.AddRange(await ReadFileAsync(file, batchId, cancellationToken));
        }

        return rows;
    }

    private async Task<List<StageOpinion>> ReadFileAsync(string path, Guid batchId, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(path);
        var rows = new List<StageOpinion>();
        var rejected = 0;

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            BadDataFound = args => logger.LogWarning(
                "{File}: dato mal formado en la fila {Row}: {Raw}", fileName, args.Context.Parser?.Row, args.RawRecord),
            MissingFieldFound = args => logger.LogWarning(
                "{File}: falta el campo {Field} en la fila {Row}",
                fileName, args.HeaderNames?.FirstOrDefault() ?? args.Index.ToString(), args.Context.Parser?.Row),
            ReadingExceptionOccurred = args =>
            {
                rejected++;
                logger.LogWarning(args.Exception, "{File}: fila {Row} omitida por error de lectura",
                    fileName, args.Exception.Context?.Parser?.Row);
                return false; // no lanzar: se omite la fila
            }
        };

        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, configuration);
        csv.Context.RegisterClassMap<SurveyCsvMap>();

        try
        {
            await foreach (var record in csv.GetRecordsAsync<SurveyCsvRecord>(cancellationToken))
            {
                var row = csv.Parser.Row;
                if (TryValidate(record, out var reason))
                {
                    rows.Add(ToStage(record, batchId));
                }
                else
                {
                    rejected++;
                    logger.LogWarning("{File}: fila {Row} omitida: {Reason}", fileName, row, reason);
                }
            }
        }
        catch (HeaderValidationException ex)
        {
            // Estructura inválida: se descarta el archivo completo, pero los demás siguen.
            logger.LogError(ex, "{File}: los encabezados no coinciden con el formato esperado; archivo omitido", fileName);
            return [];
        }

        logger.LogInformation("{File}: {Rows} filas leídas, {Rejected} omitidas", fileName, rows.Count, rejected);
        return rows;
    }

    // Solo se valida que la fila tenga ID y quepa en staging; las reglas de negocio son de la Transformación.
    private static bool TryValidate(SurveyCsvRecord r, out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(r.IdOpinion))
        {
            reason = "IdOpinion vacío";
        }
        else if (TooLong(r.IdOpinion, StageOpinionLimits.OriginalId)
                 || TooLong(r.IdCliente, StageOpinionLimits.OriginalId)
                 || TooLong(r.IdProducto, StageOpinionLimits.OriginalId))
        {
            reason = $"algún ID supera {StageOpinionLimits.OriginalId} caracteres";
        }
        else if (TooLong(r.Fecha, StageOpinionLimits.OriginalDate))
        {
            reason = "Fecha demasiado larga";
        }
        else if (TooLong(r.Clasificacion, StageOpinionLimits.OriginalClassification))
        {
            reason = "Clasificación demasiado larga";
        }
        else if (TooLong(r.PuntajeSatisfaccion, StageOpinionLimits.OriginalScore))
        {
            reason = "PuntajeSatisfacción demasiado largo";
        }
        else if (TooLong(r.Fuente, StageOpinionLimits.SourceName))
        {
            reason = "Fuente demasiado larga";
        }

        return reason.Length == 0;
    }

    private static bool TooLong(string? value, int max) => value is not null && value.Length > max;

    private static StageOpinion ToStage(SurveyCsvRecord r, Guid batchId) => new()
    {
        BatchId = batchId,
        Origin = OpinionOrigin.Survey,
        OriginalOpinionId = r.IdOpinion!,
        OriginalClientId = r.IdCliente,
        OriginalProductId = r.IdProducto,
        Comment = r.Comentario,
        OriginalDate = r.Fecha,
        OriginalClassification = r.Clasificacion,
        OriginalScore = r.PuntajeSatisfaccion,
        SourceName = r.Fuente,
        OriginalDataSourceId = null // las encuestas del CSV no traen Data_Source de ETL_Opinion
    };
}
