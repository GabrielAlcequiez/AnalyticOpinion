using AnalyticOpinion.Data.Models;

namespace AnalyticOpinion.Data.Interfaces;
public interface IExtractor
{
    string Name { get; }

    Task<IReadOnlyList<StageOpinion>> ExtractAsync(Guid batchId, CancellationToken cancellationToken);
}
