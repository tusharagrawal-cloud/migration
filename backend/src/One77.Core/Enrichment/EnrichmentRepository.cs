using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;

namespace One77.Core.Enrichment
{
    /// <summary>
    /// Enrichment-specific data access. Deliberately not a generic
    /// repository — one explicit parameterized query per operation against
    /// dbo.ProductEnrichment and its three child tables. Each method opens
    /// and closes its own connection, matching every other repository in
    /// this migration.
    /// </summary>
    public sealed class EnrichmentRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EnrichmentRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string Columns =
            "Id, ShopifyProductId, Category, IsActive, Calibre, PowerplantType, WeightGrains, RecommendedPelletWeightMin, RecommendedPelletWeightMax, CreatedAt, UpdatedAt";

        private sealed class SpecRow
        {
            public int ProductEnrichmentId { get; set; }
            public string SpecKey { get; set; }
            public string SpecValue { get; set; }
            public int SortOrder { get; set; }
        }

        public async Task<bool> ShopifyIdExistsAsync(string shopifyProductId)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.ProductEnrichment WHERE ShopifyProductId = @ShopifyProductId;",
                    new { ShopifyProductId = shopifyProductId });
                return count > 0;
            }
        }

        public async Task<EnrichmentRecord> GetByShopifyIdAsync(string shopifyProductId)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var row = await connection.QueryFirstOrDefaultAsync<EnrichmentRecord>(
                    "SELECT " + Columns + " FROM dbo.ProductEnrichment WHERE ShopifyProductId = @ShopifyProductId;",
                    new { ShopifyProductId = shopifyProductId });
                if (row == null)
                {
                    return null;
                }
                await AttachChildrenAsync(connection, row);
                return row;
            }
        }

        public async Task<List<EnrichmentRecord>> GetAllAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = (await connection.QueryAsync<EnrichmentRecord>(
                    "SELECT " + Columns + " FROM dbo.ProductEnrichment ORDER BY ShopifyProductId;")).ToList();
                if (rows.Count == 0)
                {
                    return rows;
                }

                var ids = rows.Select(r => r.Id).ToList();
                var specRows = (await connection.QueryAsync<SpecRow>(
                    "SELECT ProductEnrichmentId, SpecKey, SpecValue, SortOrder FROM dbo.ProductSpecifications WHERE ProductEnrichmentId IN @Ids ORDER BY SortOrder;",
                    new { Ids = ids })).ToList();
                var powerplantRows = (await connection.QueryAsync<(int ProductEnrichmentId, string Powerplant)>(
                    "SELECT ProductEnrichmentId, Powerplant FROM dbo.ProductCompatiblePowerplants WHERE ProductEnrichmentId IN @Ids;",
                    new { Ids = ids })).ToList();
                var useCaseRows = (await connection.QueryAsync<(int ProductEnrichmentId, string UseCase)>(
                    "SELECT ProductEnrichmentId, UseCase FROM dbo.ProductUseCases WHERE ProductEnrichmentId IN @Ids;",
                    new { Ids = ids })).ToList();

                var specsById = specRows.GroupBy(r => r.ProductEnrichmentId)
                    .ToDictionary(g => g.Key, g => g.Select(r => new EnrichmentSpecification { SpecKey = r.SpecKey, SpecValue = r.SpecValue, SortOrder = r.SortOrder }).ToList());
                var powerplantsById = powerplantRows.GroupBy(r => r.ProductEnrichmentId).ToDictionary(g => g.Key, g => g.Select(r => r.Powerplant).ToList());
                var useCasesById = useCaseRows.GroupBy(r => r.ProductEnrichmentId).ToDictionary(g => g.Key, g => g.Select(r => r.UseCase).ToList());

                foreach (var row in rows)
                {
                    row.Specifications = specsById.TryGetValue(row.Id, out var s) ? s : new List<EnrichmentSpecification>();
                    row.CompatiblePowerplants = powerplantsById.TryGetValue(row.Id, out var p) ? p : new List<string>();
                    row.UseCases = useCasesById.TryGetValue(row.Id, out var u) ? u : new List<string>();
                }
                return rows;
            }
        }

        private static async Task AttachChildrenAsync(System.Data.IDbConnection connection, EnrichmentRecord row)
        {
            row.Specifications = (await connection.QueryAsync<SpecRow>(
                "SELECT ProductEnrichmentId, SpecKey, SpecValue, SortOrder FROM dbo.ProductSpecifications WHERE ProductEnrichmentId = @Id ORDER BY SortOrder;",
                new { row.Id }))
                .Select(r => new EnrichmentSpecification { SpecKey = r.SpecKey, SpecValue = r.SpecValue, SortOrder = r.SortOrder })
                .ToList();
            row.CompatiblePowerplants = (await connection.QueryAsync<string>(
                "SELECT Powerplant FROM dbo.ProductCompatiblePowerplants WHERE ProductEnrichmentId = @Id;", new { row.Id })).ToList();
            row.UseCases = (await connection.QueryAsync<string>(
                "SELECT UseCase FROM dbo.ProductUseCases WHERE ProductEnrichmentId = @Id;", new { row.Id })).ToList();
        }

        public async Task<int> InsertAsync(EnrichmentRecord record)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.ProductEnrichment
                        (ShopifyProductId, Category, IsActive, Calibre, PowerplantType, WeightGrains, RecommendedPelletWeightMin, RecommendedPelletWeightMax)
                      OUTPUT INSERTED.Id
                      VALUES
                        (@ShopifyProductId, @Category, @IsActive, @Calibre, @PowerplantType, @WeightGrains, @RecommendedPelletWeightMin, @RecommendedPelletWeightMax);",
                    record);
            }
        }

        public async Task UpdateCoreFieldsAsync(EnrichmentRecord record)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    @"UPDATE dbo.ProductEnrichment
                      SET Category = @Category, IsActive = @IsActive, Calibre = @Calibre, PowerplantType = @PowerplantType,
                          WeightGrains = @WeightGrains, RecommendedPelletWeightMin = @RecommendedPelletWeightMin,
                          RecommendedPelletWeightMax = @RecommendedPelletWeightMax, UpdatedAt = SYSUTCDATETIME()
                      WHERE Id = @Id;",
                    record);
            }
        }

        public async Task ReplaceSpecificationsAsync(int enrichmentId, List<EnrichmentSpecification> specifications)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.ProductSpecifications WHERE ProductEnrichmentId = @Id;", new { Id = enrichmentId });
                if (specifications != null && specifications.Count > 0)
                {
                    var rows = specifications.Select(s => new { Id = enrichmentId, s.SpecKey, s.SpecValue, s.SortOrder });
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.ProductSpecifications (ProductEnrichmentId, SpecKey, SpecValue, SortOrder) VALUES (@Id, @SpecKey, @SpecValue, @SortOrder);",
                        rows);
                }
            }
        }

        public async Task ReplaceCompatiblePowerplantsAsync(int enrichmentId, List<string> powerplants)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.ProductCompatiblePowerplants WHERE ProductEnrichmentId = @Id;", new { Id = enrichmentId });
                if (powerplants != null && powerplants.Count > 0)
                {
                    var rows = powerplants.Distinct().Select(p => new { Id = enrichmentId, Powerplant = p });
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.ProductCompatiblePowerplants (ProductEnrichmentId, Powerplant) VALUES (@Id, @Powerplant);", rows);
                }
            }
        }

        public async Task ReplaceUseCasesAsync(int enrichmentId, List<string> useCases)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.ProductUseCases WHERE ProductEnrichmentId = @Id;", new { Id = enrichmentId });
                if (useCases != null && useCases.Count > 0)
                {
                    var rows = useCases.Distinct().Select(uc => new { Id = enrichmentId, UseCase = uc });
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.ProductUseCases (ProductEnrichmentId, UseCase) VALUES (@Id, @UseCase);", rows);
                }
            }
        }
    }
}
