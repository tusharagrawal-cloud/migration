using System;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;
using One77.Core.Enrichment;

namespace One77.Core.Tests.Enrichment
{
    /// <summary>
    /// Shared base for Milestone 7 Product Enrichment integration tests.
    /// Exercises the real EnrichmentRepository/EnrichmentService against the
    /// REAL SQL Server 2019 instance named by ONE77_TEST_CONNECTION_STRING —
    /// never a mock. Fake Shopify-format GIDs only; never real store data.
    /// </summary>
    public abstract class EnrichmentTestBase
    {
        protected static string? ConnectionString =>
            Environment.GetEnvironmentVariable("ONE77_TEST_CONNECTION_STRING");

        protected static bool HasDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

        protected static EnrichmentRepository CreateRepository() =>
            new(new SqlConnectionFactory(ConnectionString!));

        protected static EnrichmentService CreateService(EnrichmentRepository? repository = null) =>
            new(repository ?? CreateRepository());

        protected static string Gid(string prefix) => $"gid://shopify/Product/test-{prefix}-{Guid.NewGuid():N}".Substring(0, 60);

        protected static async Task CleanupAsync(params string[] shopifyProductIds)
        {
            using var connection = await OpenRawAsync();
            foreach (var id in shopifyProductIds)
            {
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.ProductUseCases WHERE ProductEnrichmentId IN (SELECT Id FROM dbo.ProductEnrichment WHERE ShopifyProductId = @Id);", new { Id = id });
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.ProductCompatiblePowerplants WHERE ProductEnrichmentId IN (SELECT Id FROM dbo.ProductEnrichment WHERE ShopifyProductId = @Id);", new { Id = id });
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.ProductSpecifications WHERE ProductEnrichmentId IN (SELECT Id FROM dbo.ProductEnrichment WHERE ShopifyProductId = @Id);", new { Id = id });
                await connection.ExecuteAsync("DELETE FROM dbo.ProductEnrichment WHERE ShopifyProductId = @Id;", new { Id = id });
            }
        }

        private static async Task<System.Data.IDbConnection> OpenRawAsync()
        {
            var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
    }
}
