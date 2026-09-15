using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Schema
{
    public class LearnSchemaTests : SchemaTestBase
    {
        private readonly ITestOutputHelper _output;
        public LearnSchemaTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task CanInsert_CategoryAndEntry()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var slug = Unique("airgun-basics");
            try
            {
                var categoryId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.LearnCategories (Name, Slug) OUTPUT INSERTED.Id VALUES (@Name, @Slug);",
                    new { Name = "Airgun Basics", Slug = slug });

                var entryId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.LearnEntries (LearnCategoryId, Title, Body) OUTPUT INSERTED.Id VALUES (@CategoryId, @Title, @Body);",
                    new { CategoryId = categoryId, Title = "Choosing your first airgun", Body = "Plain text content." });

                Assert.True(categoryId > 0);
                Assert.True(entryId > 0);
            }
            finally
            {
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.LearnEntries WHERE LearnCategoryId IN (SELECT Id FROM dbo.LearnCategories WHERE Slug = @Slug);",
                    new { Slug = slug });
                await connection.ExecuteAsync("DELETE FROM dbo.LearnCategories WHERE Slug = @Slug;", new { Slug = slug });
            }
        }

        [Fact]
        public async Task OrphanEntry_IsRejectedByForeignKey()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.LearnEntries (LearnCategoryId, Title, Body) VALUES (-999999, 'Orphan Entry', 'Body text');"));

            Assert.Contains("FK_LearnEntries_Category", ex.Message);
        }

        [Theory]
        [InlineData("Draft")]
        [InlineData("Live")]
        [InlineData("Hidden")]
        public async Task ValidStatuses_AreAccepted_OnCategory(string status)
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var slug = Unique($"status-{status}");
            try
            {
                var id = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.LearnCategories (Name, Slug, Status) OUTPUT INSERTED.Id VALUES (@Name, @Slug, @Status);",
                    new { Name = "Status Test", Slug = slug, Status = status });

                Assert.True(id > 0);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.LearnCategories WHERE Slug = @Slug;", new { Slug = slug });
            }
        }

        [Fact]
        public async Task InvalidStatus_IsRejected_OnCategory()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var slug = Unique("bad-status");

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.LearnCategories (Name, Slug, Status) VALUES (@Name, @Slug, 'Published');",
                    new { Name = "Bad Status", Slug = slug }));

            Assert.Contains("CK_LearnCategories_Status", ex.Message);
        }

        [Fact]
        public async Task InvalidStatus_IsRejected_OnEntry()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var slug = Unique("bad-entry-status-parent");
            try
            {
                var categoryId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.LearnCategories (Name, Slug) OUTPUT INSERTED.Id VALUES (@Name, @Slug);",
                    new { Name = "Parent", Slug = slug });

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.LearnEntries (LearnCategoryId, Title, Body, Status) VALUES (@CategoryId, 'Bad Entry', 'Body', 'Archived');",
                        new { CategoryId = categoryId }));

                Assert.Contains("CK_LearnEntries_Status", ex.Message);
            }
            finally
            {
                await connection.ExecuteAsync("DELETE FROM dbo.LearnCategories WHERE Slug = @Slug;", new { Slug = slug });
            }
        }
    }
}
