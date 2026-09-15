using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace One77.Core.Data
{
    /// <summary>
    /// Default <see cref="IDbConnectionFactory"/> backed by Microsoft.Data.SqlClient.
    /// The connection string is supplied by the host (dev host today, whatever
    /// production host is chosen at the Section 11 checkpoint later) — this
    /// class has no knowledge of where it's hosted.
    /// </summary>
    public sealed class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string must not be null or empty.", nameof(connectionString));
            }

            _connectionString = connectionString;
        }

        public async Task<IDbConnection> OpenConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
    }
}
