using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;

namespace One77.Core.Bundles
{
    /// <summary>
    /// Bundle-specific data access. Deliberately not a generic repository —
    /// one explicit parameterized query per operation against dbo.Bundles
    /// and dbo.BundleItems. Each method opens and closes its own connection.
    /// </summary>
    public sealed class BundleRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BundleRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string BundleColumns = "Id, Name, Tagline, Status, SortPriority, CreatedAt, UpdatedAt";
        private const string ItemColumns = "Id, BundleId, ShopifyProductId, ItemRole, SortOrder";

        private sealed class ItemRow
        {
            public int Id { get; set; }
            public int BundleId { get; set; }
            public string ShopifyProductId { get; set; }
            public string ItemRole { get; set; }
            public int SortOrder { get; set; }
        }

        public async Task<List<Bundle>> GetAllAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var bundles = (await connection.QueryAsync<Bundle>(
                    "SELECT " + BundleColumns + " FROM dbo.Bundles ORDER BY SortPriority, Name;")).ToList();
                await AttachItemsAsync(connection, bundles);
                return bundles;
            }
        }

        public async Task<List<Bundle>> GetPublishedAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var bundles = (await connection.QueryAsync<Bundle>(
                    "SELECT " + BundleColumns + " FROM dbo.Bundles WHERE Status = @Status ORDER BY SortPriority, Name;",
                    new { Status = BundleStatus.Published })).ToList();
                await AttachItemsAsync(connection, bundles);
                return bundles;
            }
        }

        public async Task<Bundle> GetByIdAsync(int id)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var bundle = await connection.QueryFirstOrDefaultAsync<Bundle>(
                    "SELECT " + BundleColumns + " FROM dbo.Bundles WHERE Id = @Id;", new { Id = id });
                if (bundle == null)
                {
                    return null;
                }
                bundle.Items = (await connection.QueryAsync<BundleItem>(
                    "SELECT " + ItemColumns + " FROM dbo.BundleItems WHERE BundleId = @Id ORDER BY SortOrder;", new { Id = id })).ToList();
                return bundle;
            }
        }

        private static async Task AttachItemsAsync(System.Data.IDbConnection connection, List<Bundle> bundles)
        {
            if (bundles.Count == 0)
            {
                return;
            }
            var ids = bundles.Select(b => b.Id).ToList();
            var itemRows = (await connection.QueryAsync<ItemRow>(
                "SELECT " + ItemColumns + " FROM dbo.BundleItems WHERE BundleId IN @Ids ORDER BY BundleId, SortOrder;", new { Ids = ids })).ToList();
            var itemsByBundleId = itemRows.GroupBy(r => r.BundleId).ToDictionary(
                g => g.Key,
                g => g.Select(r => new BundleItem { Id = r.Id, ShopifyProductId = r.ShopifyProductId, ItemRole = r.ItemRole, SortOrder = r.SortOrder }).ToList());

            foreach (var bundle in bundles)
            {
                bundle.Items = itemsByBundleId.TryGetValue(bundle.Id, out var items) ? items : new List<BundleItem>();
            }
        }

        public async Task<int> InsertBundleAsync(string name, string tagline, string status, int sortPriority)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.Bundles (Name, Tagline, Status, SortPriority)
                      OUTPUT INSERTED.Id
                      VALUES (@Name, @Tagline, @Status, @SortPriority);",
                    new { Name = name, Tagline = tagline, Status = status, SortPriority = sortPriority });
            }
        }

        public async Task UpdateBundleCoreAsync(int id, string name, string tagline, string status, int sortPriority)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    @"UPDATE dbo.Bundles
                      SET Name = @Name, Tagline = @Tagline, Status = @Status, SortPriority = @SortPriority, UpdatedAt = SYSUTCDATETIME()
                      WHERE Id = @Id;",
                    new { Id = id, Name = name, Tagline = tagline, Status = status, SortPriority = sortPriority });
            }
        }

        public async Task SetStatusAsync(int id, string status)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    "UPDATE dbo.Bundles SET Status = @Status, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;",
                    new { Id = id, Status = status });
            }
        }

        /// <summary>Full replace of a bundle's items, ordered by their position in the list (SortOrder = index).</summary>
        public async Task ReplaceItemsAsync(int bundleId, List<BundleItemSpec> items)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync("DELETE FROM dbo.BundleItems WHERE BundleId = @Id;", new { Id = bundleId });
                if (items != null && items.Count > 0)
                {
                    var rows = items.Select((item, index) => new
                    {
                        BundleId = bundleId,
                        item.ShopifyProductId,
                        item.ItemRole,
                        SortOrder = index
                    });
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.BundleItems (BundleId, ShopifyProductId, ItemRole, SortOrder) VALUES (@BundleId, @ShopifyProductId, @ItemRole, @SortOrder);",
                        rows);
                }
            }
        }
    }
}
