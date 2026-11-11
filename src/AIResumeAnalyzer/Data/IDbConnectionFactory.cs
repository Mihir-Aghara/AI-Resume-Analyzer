using System.Data;
using Microsoft.Data.SqlClient;

namespace AIResumeAnalyzer.Data
{
    public interface IDbConnectionFactory
    {
        SqlConnection CreateConnection();
        Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
    }
}
