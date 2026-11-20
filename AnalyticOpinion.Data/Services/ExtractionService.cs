using System.Diagnostics;
using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Models;
using Microsoft.Extensions.Logging;

namespace AnalyticOpinion.Data.Services;

// Vacía la staging y corre las fuentes en paralelo; si una falla, las demás siguen.
public sealed class ExtractionService(
    IEnumerable<IExtractor> extractors,
    IStagingRepository stagingRepository,
    ILogger<ExtractionService> logger)
{
    public async Task<ExtractionRunSummary> RunAsync(CancellationToken cancellationToken)
    {
        var batchId = Guid.NewGuid();
        var total = Stopwatch.StartNew();

        using var scope = logger.BeginScope(new Dictionary<string, object> { ["BatchId"] = batchId });
        logger.LogInformation("Iniciando extracción. Lote {BatchId}", batchId);

        await stagingRepository.TruncateAsync(cancellationToken);

        var results = await Task.WhenAll(extractors.Select(e => Task.Run(() => RunExtractorAsync(e, batchId, cancellationToken), cancellationToken)));

        total.Stop();
        var summary = new ExtractionRunSummary(batchId, results, total.Elapsed);
        LogSummary(summary);
        return summary;
    }

    private async Task<ExtractionResult> RunExtractorAsync(IExtractor extractor, Guid batchId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var extracted = 0;

        try
        {
            logger.LogInformation("[{Source}] Extracción iniciada", extractor.Name);

            var rows = await extractor.ExtractAsync(batchId, cancellationToken);
            extracted = rows.Count;
            var extractTime = stopwatch.Elapsed;

            var staged = await stagingRepository.BulkInsertAsync(rows.ToList(), cancellationToken);
            stopwatch.Stop();

            logger.LogInformation(
                "[{Source}] {Extracted} filas extraídas en {ExtractMs} ms; {Staged} insertadas en staging en {StageMs} ms",
                extractor.Name, extracted, (long)extractTime.TotalMilliseconds, staged,
                (long)(stopwatch.Elapsed - extractTime).TotalMilliseconds);

            return new ExtractionResult(extractor.Name, extracted, staged, stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "[{Source}] Falló la extracción tras {ElapsedMs} ms", extractor.Name, stopwatch.ElapsedMilliseconds);
            return new ExtractionResult(extractor.Name, extracted, 0, stopwatch.Elapsed, ex.Message);
        }
    }

    private void LogSummary(ExtractionRunSummary summary)
    {
        foreach (var r in summary.Results)
        {
            logger.LogInformation("Resumen | {Source,-28} | {Status,-5} | extraídas {Extracted,5} | staging {Staged,5} | {ElapsedMs,6} ms",
                r.Source, r.Succeeded ? "OK" : "ERROR", r.RowsExtracted, r.RowsStaged, (long)r.Elapsed.TotalMilliseconds);
        }

        var level = summary.AllSucceeded ? LogLevel.Information : LogLevel.Warning;
        logger.Log(level, "Extracción finalizada. Lote {BatchId}: {Total} filas en staging, {Failed} fuente(s) con error, {ElapsedMs} ms",
            summary.BatchId, summary.TotalRowsStaged, summary.Results.Count(r => !r.Succeeded), (long)summary.TotalElapsed.TotalMilliseconds);
    }
}
