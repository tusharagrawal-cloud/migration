using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Schema
{
    public class ProductEnrichmentSchemaTests : SchemaTestBase
    {
        private readonly ITestOutputHelper _output;
        public ProductEnrichmentSchemaTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task CanInsert_ValidShopifyProductReference()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var shopifyId = Unique("gid://shopify/Product/1");
            try
            {
                var id = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.ProductEnrichment (ShopifyProductId, Category) OUTPUT INSERTED.Id VALUES (@ShopifyProductId, 'airgun');",
                    new { ShopifyProductId = shopifyId });

                Assert.True(id > 0);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.ProductEnrichment WHERE ShopifyProductId = @ShopifyProductId;", new { ShopifyProductId = shopifyId });
            }
        }

        [Fact]
        public async Task DuplicateShopifyProductId_IsRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var shopifyId = Unique("gid://shopify/Product/dup");
            try
            {
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.ProductEnrichment (ShopifyProductId, Category) VALUES (@ShopifyProductId, 'pellet');",
                    new { ShopifyProductId = shopifyId });

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.ProductEnrichment (ShopifyProductId, Category) VALUES (@ShopifyProductId, 'pellet');",
                        new { ShopifyProductId = shopifyId }));

                Assert.Contains("UQ_ProductEnrichment_ShopifyProductId", ex.Message);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.ProductEnrichment WHERE ShopifyProductId = @ShopifyProductId;", new { ShopifyProductId = shopifyId });
            }
        }

        [Fact]
        public async Task InvalidCategory_IsRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var shopifyId = Unique("gid://shopify/Product/badcat");

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.ProductEnrichment (ShopifyProductId, Category) VALUES (@ShopifyProductId, 'spaceship');",
                    new { ShopifyProductId = shopifyId }));

            Assert.Contains("CK_ProductEnrichment_Category", ex.Message);
        }

        [Fact]
        public async Task CanAttach_MultipleOrderedSpecifications()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var shopifyId = Unique("gid://shopify/Product/specs");
            try
            {
                var enrichmentId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.ProductEnrichment (ShopifyProductId, Category) OUTPUT INSERTED.Id VALUES (@ShopifyProductId, 'airgun');",
                    new { ShopifyProductId = shopifyId });

                await connection.ExecuteAsync(
                    @"INSERT INTO dbo.ProductSpecifications (ProductEnrichmentId, SpecKey, SpecValue, SortOrder) VALUES
                      (@Id, 'sights', 'Open sights', 0),
                      (@Id, 'sights', 'Fibre optic', 1),
                      (@Id, 'barrel_type', 'Rifled steel', 0);",
                    new { Id = enrichmentId });

                var specs = await connection.QueryAsync<(string SpecKey, string SpecValue, int SortOrder)>(
                    "SELECT SpecKey, SpecValue, SortOrder FROM dbo.ProductSpecifications WHERE ProductEnrichmentId = @Id ORDER BY SpecKey, SortOrder;",
                    new { Id = enrichmentId });

                var list = new System.Collections.Generic.List<(string, string, int)>(specs);
                Assert.Equal(3, list.Count);
                Assert.Equal(2, list.FindAll(s => s.Item1 == "sights").Count);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.ProductEnrichment WHERE ShopifyProductId = @ShopifyProductId;", new { ShopifyProductId = shopifyId });
            }
        }

        [Fact]
        public async Task OrphanSpecification_IsRejectedByForeignKey()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.ProductSpecifications (ProductEnrichmentId, SpecKey, SpecValue) VALUES (-999999, 'orphan', 'value');"));

            Assert.Contains("FK_ProductSpecifications_Enrichment", ex.Message);
        }

        [Fact]
        public async Task DeletingEnrichment_CascadesToSpecificationsAndCompatiblePowerplants()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var shopifyId = Unique("gid://shopify/Product/cascade");

            var enrichmentId = await connection.ExecuteScalarAsync<int>(
                "INSERT INTO dbo.ProductEnrichment (ShopifyProductId, Category) OUTPUT INSERTED.Id VALUES (@ShopifyProductId, 'accessory');",
                new { ShopifyProductId = shopifyId });

            await connection.ExecuteAsync(
                "INSERT INTO dbo.ProductSpecifications (ProductEnrichmentId, SpecKey, SpecValue) VALUES (@Id, 'material', 'Aluminium');",
                new { Id = enrichmentId });
            await connection.ExecuteAsync(
                "INSERT INTO dbo.ProductCompatiblePowerplants (ProductEnrichmentId, Powerplant) VALUES (@Id, 'pcp'), (@Id, 'springer');",
                new { Id = enrichmentId });
            await connection.ExecuteAsync(
                "INSERT INTO dbo.ProductUseCases (ProductEnrichmentId, UseCase) VALUES (@Id, 'plinking');",
                new { Id = enrichmentId });

            await connection.ExecuteAsync("DELETE FROM dbo.ProductEnrichment WHERE Id = @Id;", new { Id = enrichmentId });

            var remainingSpecs = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM dbo.ProductSpecifications WHERE ProductEnrichmentId = @Id;", new { Id = enrichmentId });
            var remainingPowerplants = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM dbo.ProductCompatiblePowerplants WHERE ProductEnrichmentId = @Id;", new { Id = enrichmentId });
            var remainingUseCases = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM dbo.ProductUseCases WHERE ProductEnrichmentId = @Id;", new { Id = enrichmentId });

            Assert.Equal(0, remainingSpecs);
            Assert.Equal(0, remainingPowerplants);
            Assert.Equal(0, remainingUseCases);
        }

        [Fact]
        public async Task CanAttach_MultipleUseCases()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var shopifyId = Unique("gid://shopify/Product/usecases");
            try
            {
                var enrichmentId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.ProductEnrichment (ShopifyProductId, Category) OUTPUT INSERTED.Id VALUES (@ShopifyProductId, 'airgun');",
                    new { ShopifyProductId = shopifyId });

                await connection.ExecuteAsync(
                    "INSERT INTO dbo.ProductUseCases (ProductEnrichmentId, UseCase) VALUES (@Id, 'target_10m'), (@Id, 'plinking');",
                    new { Id = enrichmentId });

                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.ProductUseCases WHERE ProductEnrichmentId = @Id;", new { Id = enrichmentId });

                Assert.Equal(2, count);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.ProductEnrichment WHERE ShopifyProductId = @ShopifyProductId;", new { ShopifyProductId = shopifyId });
            }
        }

        [Fact]
        public async Task OrphanUseCase_IsRejectedByForeignKey()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.ProductUseCases (ProductEnrichmentId, UseCase) VALUES (-999999, 'plinking');"));

            Assert.Contains("FK_ProductUseCases_Enrichment", ex.Message);
        }
    }
}
