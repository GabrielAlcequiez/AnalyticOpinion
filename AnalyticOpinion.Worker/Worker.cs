using AnalyticOpinion.Data.Services;

namespace AnalyticOpinion.Worker
{
    // Corre la extracción una sola vez y detiene el host. ExitCode = 1 si alguna fuente falló.
    public class Worker(
        ExtractionService extractionService,
        IHostApplicationLifetime lifetime,
        ILogger<Worker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var summary = await extractionService.RunAsync(stoppingToken);
                Environment.ExitCode = summary.AllSucceeded ? 0 : 1;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Extracción cancelada");
                Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error fatal durante la extracción");
                Environment.ExitCode = 1;
            }
            finally
            {
                lifetime.StopApplication();
            }
        }
    }
}
