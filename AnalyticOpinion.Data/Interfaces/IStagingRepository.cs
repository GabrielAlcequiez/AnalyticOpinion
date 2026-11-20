using AnalyticOpinion.Data.Models;

namespace AnalyticOpinion.Data.Interfaces;

// persistencia de la tabla de staging
public interface IStagingRepository
{
    Task TruncateAsync(CancellationToken cancellationToken);

    Task<int> BulkInsertAsync(IReadOnlyCollection<StageOpinion> rows, CancellationToken cancellationToken);
}
