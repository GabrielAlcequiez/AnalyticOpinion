using System.Security.Cryptography;
using System.Text;
using AnalyticOpinion.Data.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AnalyticOpinion.API.Security;

// Valida el header X-Api-Key. Si no hay clave configurada se rechaza todo (fail closed).
public sealed class ApiKeyAuthorizationFilter(IConfiguration configuration, ILogger<ApiKeyAuthorizationFilter> logger)
    : IAuthorizationFilter
{
    public const string ConfigurationKey = "ApiSecurity:ApiKey";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expected = configuration[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(expected))
        {
            logger.LogError("{Key} no está configurada; se rechaza la solicitud", ConfigurationKey);
            context.Result = new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            return;
        }

        var provided = context.HttpContext.Request.Headers[ApiKeyDefaults.HeaderName].ToString();
        if (provided.Length == 0 || !FixedTimeEquals(provided, expected))
        {
            logger.LogWarning("Solicitud rechazada: API key ausente o inválida desde {RemoteIp}",
                context.HttpContext.Connection.RemoteIpAddress);
            context.Result = new UnauthorizedResult();
        }
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
