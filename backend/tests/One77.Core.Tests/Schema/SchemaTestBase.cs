using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace One77.Core.Tests.Schema
{
    /// <summary>
    /// Shared base for Milestone 2 schema integration tests. Every test in
    /// this folder runs against the REAL SQL Server 2019 instance named by
    /// ONE77_TEST_CONNECTION_STRING (expected to point at the "One77"
    /// database the Milestone 2 schema scripts were applied to) — never an
    /// in-memory substitute. If the variable is absent, tests report
    /// "not executed" via test output rather than silently passing.
    /// </summary>
    public abstract class SchemaTestBase
    {
        protected static string? ConnectionString =>
            Environment.GetEnvironmentVariable("ONE77_TEST_CONNECTION_STRING");

        protected static bool HasDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

        protected static async Task<IDbConnection> OpenAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }

        protected static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}".Substring(0, Math.Min(60, prefix.Length + 33));
    }
}
