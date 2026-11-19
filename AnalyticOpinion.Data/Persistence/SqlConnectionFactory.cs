using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AnalyticOpinion.Data.Persistence;

public sealed class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    public SqlConnection CreateSourceConnection() => new(GetRequired(ConnectionNames.SourceDb));

    public SqlConnection CreateAnalyticsConnection() => new(GetRequired(ConnectionNames.AnalyticsDb));

    private string GetRequired(string name) =>
        configuration.GetConnectionString(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Falta la cadena de conexión '{name}' en ConnectionStrings.");
}
