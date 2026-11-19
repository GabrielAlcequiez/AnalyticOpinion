using Microsoft.Data.SqlClient;

namespace AnalyticOpinion.Data.Interfaces;

public interface ISqlConnectionFactory
{
    SqlConnection CreateSourceConnection();

    SqlConnection CreateAnalyticsConnection();
}
