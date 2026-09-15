using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using One77.Core.Data;
using One77.Core.Learn;

namespace One77.Core.Tests.Learn
{
    /// <summary>
    /// Shared base for Milestone 3 Learn integration tests. Exercises the
    /// real LearnRepository/LearnCategoryService/LearnEntryService classes
    /// against the REAL SQL Server 2019 instance named by
    /// ONE77_TEST_CONNECTION_STRING — never a mock or an in-memory
    /// substitute. If the variable is absent, tests report "not executed"
    /// via test output rather than silently passing.
    /// </summary>
    public abstract class LearnTestBase
    {
        protected static string? ConnectionString =>
            Environment.GetEnvironmentVariable("ONE77_TEST_CONNECTION_STRING");

        protected static bool HasDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

        protected static LearnRepository CreateRepository() =>
            new(new SqlConnectionFactory(ConnectionString!));

        protected static LearnCategoryService CreateCategoryService(LearnRepository? repository = null) =>
            new(repository ?? CreateRepository());

        protected static LearnEntryService CreateEntryService(LearnRepository? repository = null) =>
            new(repository ?? CreateRepository());

        protected static async Task<IDbConnection> OpenRawAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }

        protected static string Unique(string prefix) =>
            $"{prefix}-{Guid.NewGuid():N}".Substring(0, Math.Min(80, prefix.Length + 33));

        /// <summary>Deletes a category and its entries directly (children first), bypassing the service layer.</summary>
        protected static async Task DeleteCategoryCascadeAsync(int categoryId)
        {
            using var connection = await OpenRawAsync();
            await connection.ExecuteAsync("DELETE FROM dbo.LearnEntries WHERE LearnCategoryId = @Id;", new { Id = categoryId });
            await connection.ExecuteAsync("DELETE FROM dbo.LearnCategories WHERE Id = @Id;", new { Id = categoryId });
        }
    }
}
