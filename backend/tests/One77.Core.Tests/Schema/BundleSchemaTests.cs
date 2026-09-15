using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Schema
{
    public class BundleSchemaTests : SchemaTestBase
    {
        private readonly ITestOutputHelper _output;
        public BundleSchemaTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task CanCreate_Bundle()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var name = Unique("Starter Kit");
            try
            {
                var id = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.Bundles (Name) OUTPUT INSERTED.Id VALUES (@Name);",
                    new { Name = name });

                Assert.True(id > 0);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.Bundles WHERE Name = @Name;", new { Name = name });
            }
        }

        [Fact]
        public async Task CanAdd_OrderedItems_ToBundle()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var name = Unique("Combo Set");
            try
            {
                var bundleId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.Bundles (Name) OUTPUT INSERTED.Id VALUES (@Name);",
                    new { Name = name });

                await connection.ExecuteAsync(
                    @"INSERT INTO dbo.BundleItems (BundleId, ShopifyProductId, ItemRole, SortOrder) VALUES
                      (@Id, 'gid://shopify/Product/airgun-1', 'Primary', 0),
                      (@Id, 'gid://shopify/Product/pellet-1', 'Pellet', 1),
                      (@Id, 'gid://shopify/Product/accessory-1', 'Accessory', 2);",
                    new { Id = bundleId });

                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.BundleItems WHERE BundleId = @Id;", new { Id = bundleId });

                Assert.Equal(3, count);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.Bundles WHERE Name = @Name;", new { Name = name });
            }
        }

        [Fact]
        public async Task OrphanBundleItem_IsRejectedByForeignKey()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.BundleItems (BundleId, ShopifyProductId, ItemRole) VALUES (-999999, 'gid://shopify/Product/orphan', 'Primary');"));

            Assert.Contains("FK_BundleItems_Bundle", ex.Message);
        }

        [Fact]
        public async Task OnlyOnePrimaryItem_PerBundle_IsEnforced()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var name = Unique("Two Primaries");
            try
            {
                var bundleId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.Bundles (Name) OUTPUT INSERTED.Id VALUES (@Name);",
                    new { Name = name });

                await connection.ExecuteAsync(
                    "INSERT INTO dbo.BundleItems (BundleId, ShopifyProductId, ItemRole) VALUES (@Id, 'gid://shopify/Product/airgun-a', 'Primary');",
                    new { Id = bundleId });

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.BundleItems (BundleId, ShopifyProductId, ItemRole) VALUES (@Id, 'gid://shopify/Product/airgun-b', 'Primary');",
                        new { Id = bundleId }));

                Assert.Contains("UQ_BundleItems_OnePrimaryPerBundle", ex.Message);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.Bundles WHERE Name = @Name;", new { Name = name });
            }
        }
    }
}
