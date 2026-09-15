using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using One77.Core.Data;
using One77.Core.Match;

namespace One77.Core.Tests.Match
{
    /// <summary>
    /// Shared base for Milestone 6 Match integration tests. Exercises the
    /// real MatchRepository/MatchAdminService/MatchPublicResolver classes
    /// against the REAL SQL Server 2019 instance named by
    /// ONE77_TEST_CONNECTION_STRING — never a mock. Product/spec fixtures
    /// are seeded via direct SQL (ProductEnrichment/ProductUseCases/
    /// ProductCompatiblePowerplants), since the Product Enrichment admin API
    /// itself is a later milestone's scope — Match only ever reads that data.
    /// Fake Shopify-format GIDs only; never real store data.
    /// </summary>
    public abstract class MatchTestBase
    {
        protected static string? ConnectionString =>
            Environment.GetEnvironmentVariable("ONE77_TEST_CONNECTION_STRING");

        protected static bool HasDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

        protected static MatchRepository CreateRepository() =>
            new(new SqlConnectionFactory(ConnectionString!));

        protected static MatchAdminService CreateAdminService(MatchRepository? repository = null) =>
            new(repository ?? CreateRepository());

        protected static MatchPublicResolver CreateResolver(MatchRepository? repository = null) =>
            new(repository ?? CreateRepository());

        protected static async Task<IDbConnection> OpenRawAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }

        /// <summary>Fake, Shopify-GID-shaped test identifier — never real store data.</summary>
        protected static string Gid(string prefix) => $"gid://shopify/Product/test-{prefix}-{Guid.NewGuid():N}".Substring(0, 60);

        protected static async Task<int> InsertEnrichmentAsync(
            string shopifyProductId, string category,
            string? calibre = null, string? powerplantType = null, decimal? weightGrains = null,
            decimal? recommendedMin = null, decimal? recommendedMax = null, bool isActive = true,
            IEnumerable<string>? useCases = null, IEnumerable<string>? compatiblePowerplants = null)
        {
            using var connection = await OpenRawAsync();
            var id = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO dbo.ProductEnrichment
                    (ShopifyProductId, Category, IsActive, Calibre, PowerplantType, WeightGrains, RecommendedPelletWeightMin, RecommendedPelletWeightMax)
                  OUTPUT INSERTED.Id
                  VALUES
                    (@ShopifyProductId, @Category, @IsActive, @Calibre, @PowerplantType, @WeightGrains, @RecommendedPelletWeightMin, @RecommendedPelletWeightMax);",
                new
                {
                    ShopifyProductId = shopifyProductId,
                    Category = category,
                    IsActive = isActive,
                    Calibre = calibre,
                    PowerplantType = powerplantType,
                    WeightGrains = weightGrains,
                    RecommendedPelletWeightMin = recommendedMin,
                    RecommendedPelletWeightMax = recommendedMax
                });

            if (useCases != null)
            {
                foreach (var uc in useCases)
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.ProductUseCases (ProductEnrichmentId, UseCase) VALUES (@Id, @UseCase);",
                        new { Id = id, UseCase = uc });
                }
            }

            if (compatiblePowerplants != null)
            {
                foreach (var pp in compatiblePowerplants)
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.ProductCompatiblePowerplants (ProductEnrichmentId, Powerplant) VALUES (@Id, @Powerplant);",
                        new { Id = id, Powerplant = pp });
                }
            }

            return id;
        }

        /// <summary>Deletes any Match relationships touching these Shopify Product IDs, then the enrichment rows themselves (no formal FK links them — cleanup is purely for test-database hygiene).</summary>
        protected static async Task CleanupProductsAsync(params string[] shopifyProductIds)
        {
            using var connection = await OpenRawAsync();
            foreach (var id in shopifyProductIds)
            {
                await connection.ExecuteAsync(
                    @"DELETE FROM dbo.MatchRelationshipUseCases WHERE MatchRelationshipId IN
                        (SELECT Id FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Id OR TargetShopifyProductId = @Id);",
                    new { Id = id });
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Id OR TargetShopifyProductId = @Id;",
                    new { Id = id });
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.ProductEnrichment WHERE ShopifyProductId = @Id;", new { Id = id });
            }
        }
    }
}
