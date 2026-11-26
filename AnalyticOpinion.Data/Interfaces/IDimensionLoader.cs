namespace AnalyticOpinion.Data.Interfaces;

// Carga las tablas Dim* de OpinionAnalytics mediante stored procedures.
public interface IDimensionLoader
{
    Task LoadAsync(CancellationToken cancellationToken);
}
