using AnalyticOpinion.Data.Models;

namespace AnalyticOpinion.Data.Interfaces;

// Se expone en la API para poder paginar los comentarios sociales.
public interface ISocialCommentRepository
{
    Task<PagedResult<SocialCommentDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken);
}
