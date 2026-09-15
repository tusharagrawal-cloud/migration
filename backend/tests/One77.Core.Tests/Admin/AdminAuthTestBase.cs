using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using One77.Core.Admin;
using One77.Core.Data;

namespace One77.Core.Tests.Admin
{
    /// <summary>
    /// Shared base for Milestone 10 Admin auth integration tests. Exercises
    /// the real AdminUserRepository/AdminAuthService against the REAL SQL
    /// Server 2019 instance named by ONE77_TEST_CONNECTION_STRING — never a
    /// mock, matching every other domain's test base in this migration.
    /// </summary>
    public abstract class AdminAuthTestBase
    {
        protected static string? ConnectionString =>
            Environment.GetEnvironmentVariable("ONE77_TEST_CONNECTION_STRING");

        protected static bool HasDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

        protected static AdminUserRepository CreateRepository() =>
            new(new SqlConnectionFactory(ConnectionString!));

        protected static AdminAuthService CreateService(AdminUserRepository? repository = null) =>
            new(repository ?? CreateRepository(), new BCryptPasswordHasher());

        protected static async Task<IDbConnection> OpenRawAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }

        protected static string UniqueEmail() => $"test-{Guid.NewGuid():N}@one77.example";

        protected static async Task DeleteAdminAsync(int adminUserId)
        {
            using var connection = await OpenRawAsync();
            await connection.ExecuteAsync("DELETE FROM dbo.AdminUsers WHERE AdminUserId = @Id;", new { Id = adminUserId });
        }

        /// <summary>Deletes every AdminUsers row — used by seed-idempotency tests that need a known-empty table.</summary>
        protected static async Task DeleteAllAdminsAsync()
        {
            using var connection = await OpenRawAsync();
            await connection.ExecuteAsync("DELETE FROM dbo.AdminUsers;");
        }
    }
}
