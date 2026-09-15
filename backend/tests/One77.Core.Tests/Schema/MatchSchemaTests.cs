using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Schema
{
    public class MatchSchemaTests : SchemaTestBase
    {
        private readonly ITestOutputHelper _output;
        public MatchSchemaTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task CanInsert_CompatibleRelationship()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var source = Unique("gid://shopify/Product/src-compat");
            var target = Unique("gid://shopify/Product/tgt-compat");
            try
            {
                var id = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status)
                      OUTPUT INSERTED.Id
                      VALUES (@Source, @Target, 'pellet', 'compatible');",
                    new { Source = source, Target = target });

                Assert.True(id > 0);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Source;", new { Source = source });
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task CanInsert_RecommendedRelationship_WithValidPriority(int priority)
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var source = Unique($"gid://shopify/Product/src-rec-{priority}");
            var target = Unique($"gid://shopify/Product/tgt-rec-{priority}");
            try
            {
                var id = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status, Priority)
                      OUTPUT INSERTED.Id
                      VALUES (@Source, @Target, 'accessory', 'recommended', @Priority);",
                    new { Source = source, Target = target, Priority = priority });

                Assert.True(id > 0);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Source;", new { Source = source });
            }
        }

        [Fact]
        public async Task InvalidStatus_IsRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var source = Unique("gid://shopify/Product/src-badstatus");
            var target = Unique("gid://shopify/Product/tgt-badstatus");

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status)
                      VALUES (@Source, @Target, 'pellet', 'maybe');",
                    new { Source = source, Target = target }));

            Assert.Contains("CK_MatchRelationships_Status", ex.Message);
        }

        [Fact]
        public async Task InvalidPriority_IsRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var source = Unique("gid://shopify/Product/src-badpriority");
            var target = Unique("gid://shopify/Product/tgt-badpriority");

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status, Priority)
                      VALUES (@Source, @Target, 'accessory', 'recommended', 9);",
                    new { Source = source, Target = target }));

            Assert.Contains("CK_MatchRelationships_Priority", ex.Message);
        }

        [Fact]
        public async Task CompatibleStatus_WithNonNullPriority_IsRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var source = Unique("gid://shopify/Product/src-priorityonrecommendedonly");
            var target = Unique("gid://shopify/Product/tgt-priorityonrecommendedonly");

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status, Priority)
                      VALUES (@Source, @Target, 'pellet', 'compatible', 1);",
                    new { Source = source, Target = target }));

            Assert.Contains("CK_MatchRelationships_PriorityOnlyWhenRecommended", ex.Message);
        }

        [Fact]
        public async Task OnlyOneActiveRelationship_PerSourceTargetPair_IsEnforced()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var source = Unique("gid://shopify/Product/src-onepair");
            var target = Unique("gid://shopify/Product/tgt-onepair");
            try
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status)
                      VALUES (@Source, @Target, 'pellet', 'compatible');",
                    new { Source = source, Target = target });

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                    await connection.ExecuteAsync(
                        @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status)
                          VALUES (@Source, @Target, 'pellet', 'not_recommended');",
                        new { Source = source, Target = target }));

                Assert.Contains("UQ_MatchRelationships_ActivePair", ex.Message);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Source;", new { Source = source });
            }
        }

        [Fact]
        public async Task SoftDelete_SetsIsActiveFalse_RowStillExists()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var source = Unique("gid://shopify/Product/src-softdelete");
            var target = Unique("gid://shopify/Product/tgt-softdelete");
            try
            {
                var id = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status)
                      OUTPUT INSERTED.Id
                      VALUES (@Source, @Target, 'pellet', 'compatible');",
                    new { Source = source, Target = target });

                await connection.ExecuteAsync(
                    "UPDATE dbo.MatchRelationships SET IsActive = 0 WHERE Id = @Id;", new { Id = id });

                var isActive = await connection.ExecuteScalarAsync<bool>(
                    "SELECT IsActive FROM dbo.MatchRelationships WHERE Id = @Id;", new { Id = id });
                var rowCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.MatchRelationships WHERE Id = @Id;", new { Id = id });

                Assert.False(isActive);
                Assert.Equal(1, rowCount);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Source;", new { Source = source });
            }
        }

        [Fact]
        public async Task AfterSoftDelete_NewActiveRelationship_ForSamePair_CanBeCreated()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var source = Unique("gid://shopify/Product/src-readd");
            var target = Unique("gid://shopify/Product/tgt-readd");
            try
            {
                var firstId = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status)
                      OUTPUT INSERTED.Id
                      VALUES (@Source, @Target, 'pellet', 'compatible');",
                    new { Source = source, Target = target });

                await connection.ExecuteAsync(
                    "UPDATE dbo.MatchRelationships SET IsActive = 0 WHERE Id = @Id;", new { Id = firstId });

                var secondId = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status, Priority)
                      OUTPUT INSERTED.Id
                      VALUES (@Source, @Target, 'pellet', 'recommended', 1);",
                    new { Source = source, Target = target });

                var activeCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Source AND TargetShopifyProductId = @Target AND IsActive = 1;",
                    new { Source = source, Target = target });

                Assert.NotEqual(firstId, secondId);
                Assert.Equal(1, activeCount);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Source;", new { Source = source });
            }
        }
    }
}
