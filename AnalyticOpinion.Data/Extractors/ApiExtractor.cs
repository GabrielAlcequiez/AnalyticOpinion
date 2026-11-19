using System.Globalization;
using System.Net.Http.Json;
using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Models;
using AnalyticOpinion.Data.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AnalyticOpinion.Data.Extractors;

public sealed class ApiExtractor(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<ApiExtractor> logger) : IExtractor
{
    private const string ApiUrl = "https://localhost:7055/api/social-comments";
    private const int PageSize = 100;

    public string Name => "API REST - Redes sociales";

    public async Task<IReadOnlyList<StageOpinion>> ExtractAsync(Guid batchId, CancellationToken cancellationToken)
    {
        // La key no se escribe en el código: se guarda con "dotnet user-secrets set ApiKey <valor>".
        var apiKey = configuration["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Falta la API key en user-secrets (ApiKey).");
        }

        var rows = new List<StageOpinion>();
        var page = 1;
        PagedResult<SocialCommentDto> result;

        do
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiUrl}?page={page}&pageSize={PageSize}");
            request.Headers.Add(ApiKeyDefaults.HeaderName, apiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            result = await response.Content.ReadFromJsonAsync<PagedResult<SocialCommentDto>>(cancellationToken)
                     ?? throw new InvalidOperationException($"La API devolvió una respuesta vacía en la página {page}.");

            rows.AddRange(result.Items.Select(item => ToStage(item, batchId)));
            logger.LogDebug("API página {Page}: {Count} comentarios (acumulado {Total}/{TotalCount})",
                page, result.Items.Count, rows.Count, result.TotalCount);

            page++;
        }
        while (result.Items.Count > 0 && rows.Count < result.TotalCount);

        return rows;
    }

    private static StageOpinion ToStage(SocialCommentDto c, Guid batchId) => new()
    {
        BatchId = batchId,
        Origin = OpinionOrigin.SocialComment,
        OriginalOpinionId = c.Id.ToString(CultureInfo.InvariantCulture),
        OriginalClientId = c.ClientId?.ToString(CultureInfo.InvariantCulture),
        OriginalProductId = c.ProductId?.ToString(CultureInfo.InvariantCulture),
        Comment = c.Comment,
        OriginalDate = c.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        OriginalClassification = c.Classification,
        OriginalScore = c.Score?.ToString(CultureInfo.InvariantCulture),
        SourceName = c.Platform,
        OriginalDataSourceId = c.DataSourceId.ToString(CultureInfo.InvariantCulture)
    };
}
