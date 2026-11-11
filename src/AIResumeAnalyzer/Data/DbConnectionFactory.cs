using Microsoft.Data.SqlClient;

namespace AIResumeAnalyzer.Data
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        static DbConnectionFactory()
        {
            // Use pure managed networking to avoid native SNI.dll loading issues in modern Windows environments
            AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);
        }

        public DbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection connection string is not configured.");
        }

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = !string.IsNullOrWhiteSpace(connectionString) 
                ? connectionString 
                : throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
