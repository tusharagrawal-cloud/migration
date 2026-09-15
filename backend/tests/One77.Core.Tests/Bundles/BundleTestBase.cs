using System;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Bundles;
using One77.Core.Data;

namespace One77.Core.Tests.Bundles
{
    /// <summary>
    /// Shared base for Milestone 7 Bundle integration tests. Exercises the
    /// real BundleRepository/BundleService against the REAL SQL Server 2019
    /// instance named by ONE77_TEST_CONNECTION_STRING — never a mock. Fake
    /// Shopify-format GIDs only; never real store data.
    /// </summary>
    public abstract class BundleTestBase
    {
        protected static string? ConnectionString =>
            Environment.GetEnvironmentVariable("ONE77_TEST_CONNECTION_STRING");

        protected static bool HasDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

        protected static BundleRepository CreateRepository() =>
            new(new SqlConnectionFactory(ConnectionString!));

        protected static BundleService CreateService(BundleRepository? repository = null) =>
            new(repository ?? CreateRepository());

        protected static string Gid(string prefix) => $"gid://shopify/Product/test-{prefix}-{Guid.NewGuid():N}".Substring(0, 60);

        protected static async Task DeleteBundleAsync(int bundleId)
        {
            using var connection = await OpenRawAsync();
            await connection.ExecuteAsync("DELETE FROM dbo.BundleItems WHERE BundleId = @Id;", new { Id = bundleId });
            await connection.ExecuteAsync("DELETE FROM dbo.Bundles WHERE Id = @Id;", new { Id = bundleId });
        }

        private static async Task<System.Data.IDbConnection> OpenRawAsync()
        {
            var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
    }
}
