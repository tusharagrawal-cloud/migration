using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;

namespace One77.Core.Diagnostics
{
    /// <summary>
    /// Milestone-1-only diagnostic: proves the reusable Core layer can open a
    /// connection and execute a query against the real SQL Server instance,
    /// independent of whatever host (dev host today, production host later)
    /// is calling it. No domain schema is involved — Milestone 2 owns that.
    /// </summary>
    public sealed class DatabaseConnectivityChecker
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DatabaseConnectivityChecker(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> CanConnectAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync().ConfigureAwait(false))
            {
                var result = await connection.ExecuteScalarAsync<int>("SELECT 1;").ConfigureAwait(false);
                return result == 1;
            }
        }
    }
}
