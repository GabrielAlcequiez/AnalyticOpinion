using System.ComponentModel.DataAnnotations;
using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnalyticOpinion.API.Controllers;

[ApiController]
[Route("api/social-comments")]
[Produces("application/json")]
public sealed class SocialCommentsController(ISocialCommentRepository repository) : ControllerBase
{
    public const int MaxPageSize = 500;

    [HttpGet]
    [ProducesResponseType<PagedResult<SocialCommentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<SocialCommentDto>>> Get(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, MaxPageSize)] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetPagedAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }
}
