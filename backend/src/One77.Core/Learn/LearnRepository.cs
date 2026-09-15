using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;

namespace One77.Core.Learn
{
    /// <summary>
    /// Learn-specific data access. Deliberately not a generic repository —
    /// every method is a plain, explicit, parameterized query against the
    /// Milestone 2 LearnCategories/LearnEntries tables. Each method opens and
    /// closes its own connection, matching the pattern already established by
    /// DatabaseConnectivityChecker in Milestone 1.
    /// </summary>
    public sealed class LearnRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public LearnRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string CategoryColumns =
            "Id, Name, Slug, Description, SortPriority, Status, CreatedAt, UpdatedAt";

        private const string EntryColumns =
            "Id, LearnCategoryId AS CategoryId, Title, Body, SortPriority, Status, CreatedAt, UpdatedAt";

        private sealed class CategoryEntryCountRow
        {
            public int CategoryId { get; set; }
            public int EntryCount { get; set; }
        }

        private sealed class CategoryNameRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        // ---------- Public reads ----------

        public async Task<List<LearnCategory>> GetLiveCategoriesAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = await connection.QueryAsync<LearnCategory>(
                    "SELECT " + CategoryColumns + " FROM dbo.LearnCategories WHERE Status = 'Live' ORDER BY SortPriority, Name;");
                return rows.ToList();
            }
        }

        public async Task<LearnCategory> GetLiveCategoryBySlugAsync(string slug)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.QueryFirstOrDefaultAsync<LearnCategory>(
                    "SELECT " + CategoryColumns + " FROM dbo.LearnCategories WHERE Slug = @Slug AND Status = 'Live';",
                    new { Slug = slug });
            }
        }

        public async Task<List<LearnEntry>> GetLiveEntriesForCategoryAsync(int categoryId)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = await connection.QueryAsync<LearnEntry>(
                    "SELECT " + EntryColumns + " FROM dbo.LearnEntries WHERE LearnCategoryId = @CategoryId AND Status = 'Live' ORDER BY SortPriority, Title;",
                    new { CategoryId = categoryId });
                return rows.ToList();
            }
        }

        // ---------- Admin category reads/writes ----------

        public async Task<List<LearnCategory>> GetAllCategoriesAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = await connection.QueryAsync<LearnCategory>(
                    "SELECT " + CategoryColumns + " FROM dbo.LearnCategories ORDER BY SortPriority, Name;");
                return rows.ToList();
            }
        }

        public async Task<Dictionary<int, int>> GetEntryCountsByCategoryAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = await connection.QueryAsync<CategoryEntryCountRow>(
                    "SELECT LearnCategoryId AS CategoryId, COUNT(*) AS EntryCount FROM dbo.LearnEntries GROUP BY LearnCategoryId;");
                return rows.ToDictionary(r => r.CategoryId, r => r.EntryCount);
            }
        }

        public async Task<LearnCategory> GetCategoryByIdAsync(int id)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.QueryFirstOrDefaultAsync<LearnCategory>(
                    "SELECT " + CategoryColumns + " FROM dbo.LearnCategories WHERE Id = @Id;",
                    new { Id = id });
            }
        }

        public async Task<bool> CategorySlugExistsAsync(string slug, int? excludeId)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.LearnCategories WHERE Slug = @Slug AND (@ExcludeId IS NULL OR Id <> @ExcludeId);",
                    new { Slug = slug, ExcludeId = excludeId });
                return count > 0;
            }
        }

        public async Task<int> InsertCategoryAsync(string name, string slug, string description, int sortPriority, string status)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.LearnCategories (Name, Slug, Description, SortPriority, Status)
                      OUTPUT INSERTED.Id
                      VALUES (@Name, @Slug, @Description, @SortPriority, @Status);",
                    new { Name = name, Slug = slug, Description = description, SortPriority = sortPriority, Status = status });
            }
        }

        public async Task UpdateCategoryAsync(LearnCategory category)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    @"UPDATE dbo.LearnCategories
                      SET Name = @Name, Slug = @Slug, Description = @Description,
                          SortPriority = @SortPriority, Status = @Status, UpdatedAt = SYSUTCDATETIME()
                      WHERE Id = @Id;",
                    category);
            }
        }

        public async Task SetCategoryStatusAsync(int id, string status)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    "UPDATE dbo.LearnCategories SET Status = @Status, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;",
                    new { Id = id, Status = status });
            }
        }

        // ---------- Admin entry reads/writes ----------

        public async Task<List<LearnEntry>> GetAllEntriesAsync(int? categoryId)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = await connection.QueryAsync<LearnEntry>(
                    "SELECT " + EntryColumns + " FROM dbo.LearnEntries WHERE (@CategoryId IS NULL OR LearnCategoryId = @CategoryId) ORDER BY SortPriority, Title;",
                    new { CategoryId = categoryId });
                return rows.ToList();
            }
        }

        public async Task<Dictionary<int, string>> GetCategoryNamesAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = await connection.QueryAsync<CategoryNameRow>("SELECT Id, Name FROM dbo.LearnCategories;");
                return rows.ToDictionary(r => r.Id, r => r.Name);
            }
        }

        public async Task<LearnEntry> GetEntryByIdAsync(int id)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.QueryFirstOrDefaultAsync<LearnEntry>(
                    "SELECT " + EntryColumns + " FROM dbo.LearnEntries WHERE Id = @Id;",
                    new { Id = id });
            }
        }

        public async Task<bool> CategoryExistsAsync(int id)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.LearnCategories WHERE Id = @Id;", new { Id = id });
                return count > 0;
            }
        }

        public async Task<int> InsertEntryAsync(int categoryId, string title, string body, int sortPriority, string status)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.LearnEntries (LearnCategoryId, Title, Body, SortPriority, Status)
                      OUTPUT INSERTED.Id
                      VALUES (@CategoryId, @Title, @Body, @SortPriority, @Status);",
                    new { CategoryId = categoryId, Title = title, Body = body, SortPriority = sortPriority, Status = status });
            }
        }

        public async Task UpdateEntryAsync(LearnEntry entry)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    @"UPDATE dbo.LearnEntries
                      SET LearnCategoryId = @CategoryId, Title = @Title, Body = @Body,
                          SortPriority = @SortPriority, Status = @Status, UpdatedAt = SYSUTCDATETIME()
                      WHERE Id = @Id;",
                    entry);
            }
        }

        public async Task SetEntryStatusAsync(int id, string status)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    "UPDATE dbo.LearnEntries SET Status = @Status, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;",
                    new { Id = id, Status = status });
            }
        }
    }
}
