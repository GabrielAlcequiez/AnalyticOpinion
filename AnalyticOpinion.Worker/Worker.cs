using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Services;

namespace AnalyticOpinion.Worker
{
    // Corre el ETL una sola vez y detiene el host: Extracción -> Carga de dimensiones.
    // ExitCode = 1 si alguna fase falló.
    public class Worker(
        ExtractionService extractionService,
        IDimensionLoader dimensionLoader,
        IHostApplicationLifetime lifetime,
        ILogger<Worker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var summary = await extractionService.RunAsync(stoppingToken);
                if (!summary.AllSucceeded)
                {
                    // Integridad: no se carga nada a partir de una staging incompleta.
                    logger.LogWarning("La extracción tuvo errores; se omite la carga de dimensiones");
                    Environment.ExitCode = 1;
                    return;
                }

                await dimensionLoader.LoadAsync(stoppingToken);
                Environment.ExitCode = 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Proceso ETL cancelado");
                Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error fatal durante el proceso ETL");
                Environment.ExitCode = 1;
            }
            finally
            {
                lifetime.StopApplication();
            }
        }
    }
}
