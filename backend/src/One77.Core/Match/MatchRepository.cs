using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;

namespace One77.Core.Match
{
    /// <summary>
    /// Match-specific data access. Deliberately not a generic repository —
    /// every method is a plain, explicit, parameterized query against the
    /// Milestone 2 tables (MatchRelationships, MatchRelationshipUseCases,
    /// ProductEnrichment, ProductSpecifications, ProductCompatiblePowerplants,
    /// ProductUseCases). Each method opens and closes its own connection,
    /// matching the pattern already established by LearnRepository/
    /// WebinarRepository.
    /// </summary>
    public sealed class MatchRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MatchRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string EnrichmentColumns =
            "Id, ShopifyProductId, Category, IsActive, Calibre, PowerplantType, WeightGrains, RecommendedPelletWeightMin, RecommendedPelletWeightMax";

        private const string RelationshipColumns =
            "Id, SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status, Priority, Reason, AdminNotes, CalibreOverride, IsActive, CreatedAt, UpdatedAt";

        private sealed class EnrichmentRow
        {
            public int Id { get; set; }
            public string ShopifyProductId { get; set; }
            public string Category { get; set; }
            public bool IsActive { get; set; }
            public string Calibre { get; set; }
            public string PowerplantType { get; set; }
            public decimal? WeightGrains { get; set; }
            public decimal? RecommendedPelletWeightMin { get; set; }
            public decimal? RecommendedPelletWeightMax { get; set; }
        }

        // ---------- Product reference (ProductEnrichment + child tables) ----------

        public async Task<MatchProductRef> GetProductRefByShopifyIdAsync(string shopifyProductId)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var row = await connection.QueryFirstOrDefaultAsync<EnrichmentRow>(
                    "SELECT " + EnrichmentColumns + " FROM dbo.ProductEnrichment WHERE ShopifyProductId = @ShopifyProductId;",
                    new { ShopifyProductId = shopifyProductId });
                if (row == null)
                {
                    return null;
                }

                var useCases = (await connection.QueryAsync<string>(
                    "SELECT UseCase FROM dbo.ProductUseCases WHERE ProductEnrichmentId = @Id;", new { row.Id })).ToList();
                var powerplants = (await connection.QueryAsync<string>(
                    "SELECT Powerplant FROM dbo.ProductCompatiblePowerplants WHERE ProductEnrichmentId = @Id;", new { row.Id })).ToList();

                return ToProductRef(row, useCases, powerplants);
            }
        }

        public async Task<List<MatchProductRef>> GetProductRefsByCategoryAsync(string category, bool activeOnly)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var sql = "SELECT " + EnrichmentColumns + " FROM dbo.ProductEnrichment WHERE Category = @Category" +
                          (activeOnly ? " AND IsActive = 1" : "") + " ORDER BY ShopifyProductId;";
                var rows = (await connection.QueryAsync<EnrichmentRow>(sql, new { Category = category })).ToList();
                if (rows.Count == 0)
                {
                    return new List<MatchProductRef>();
                }

                var ids = rows.Select(r => r.Id).ToList();
                var useCaseRows = (await connection.QueryAsync<(int ProductEnrichmentId, string UseCase)>(
                    "SELECT ProductEnrichmentId, UseCase FROM dbo.ProductUseCases WHERE ProductEnrichmentId IN @Ids;", new { Ids = ids })).ToList();
                var powerplantRows = (await connection.QueryAsync<(int ProductEnrichmentId, string Powerplant)>(
                    "SELECT ProductEnrichmentId, Powerplant FROM dbo.ProductCompatiblePowerplants WHERE ProductEnrichmentId IN @Ids;", new { Ids = ids })).ToList();

                var useCasesById = useCaseRows.GroupBy(r => r.ProductEnrichmentId).ToDictionary(g => g.Key, g => g.Select(r => r.UseCase).ToList());
                var powerplantsById = powerplantRows.GroupBy(r => r.ProductEnrichmentId).ToDictionary(g => g.Key, g => g.Select(r => r.Powerplant).ToList());

                return rows.Select(r => ToProductRef(
                    r,
                    useCasesById.TryGetValue(r.Id, out var uc) ? uc : new List<string>(),
                    powerplantsById.TryGetValue(r.Id, out var pp) ? pp : new List<string>())).ToList();
            }
        }

        /// <summary>Looks up specific Shopify Product IDs at once (batched, mirrors the old system's $in-based target lookup).</summary>
        public async Task<List<MatchProductRef>> GetProductRefsByShopifyIdsAsync(IEnumerable<string> shopifyProductIds, bool activeOnly)
        {
            var ids = shopifyProductIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new List<MatchProductRef>();
            }

            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var sql = "SELECT " + EnrichmentColumns + " FROM dbo.ProductEnrichment WHERE ShopifyProductId IN @Ids" +
                          (activeOnly ? " AND IsActive = 1" : "") + ";";
                var rows = (await connection.QueryAsync<EnrichmentRow>(sql, new { Ids = ids })).ToList();
                if (rows.Count == 0)
                {
                    return new List<MatchProductRef>();
                }

                var pkIds = rows.Select(r => r.Id).ToList();
                var useCaseRows = (await connection.QueryAsync<(int ProductEnrichmentId, string UseCase)>(
                    "SELECT ProductEnrichmentId, UseCase FROM dbo.ProductUseCases WHERE ProductEnrichmentId IN @Ids;", new { Ids = pkIds })).ToList();
                var powerplantRows = (await connection.QueryAsync<(int ProductEnrichmentId, string Powerplant)>(
                    "SELECT ProductEnrichmentId, Powerplant FROM dbo.ProductCompatiblePowerplants WHERE ProductEnrichmentId IN @Ids;", new { Ids = pkIds })).ToList();

                var useCasesById = useCaseRows.GroupBy(r => r.ProductEnrichmentId).ToDictionary(g => g.Key, g => g.Select(r => r.UseCase).ToList());
                var powerplantsById = powerplantRows.GroupBy(r => r.ProductEnrichmentId).ToDictionary(g => g.Key, g => g.Select(r => r.Powerplant).ToList());

                return rows.Select(r => ToProductRef(
                    r,
                    useCasesById.TryGetValue(r.Id, out var uc) ? uc : new List<string>(),
                    powerplantsById.TryGetValue(r.Id, out var pp) ? pp : new List<string>())).ToList();
            }
        }

        private static MatchProductRef ToProductRef(EnrichmentRow row, List<string> useCases, List<string> compatiblePowerplants)
        {
            return new MatchProductRef
            {
                ShopifyProductId = row.ShopifyProductId,
                Category = row.Category,
                IsActive = row.IsActive,
                Calibre = row.Calibre,
                PowerplantType = row.PowerplantType,
                WeightGrains = row.WeightGrains,
                RecommendedPelletWeightMin = row.RecommendedPelletWeightMin,
                RecommendedPelletWeightMax = row.RecommendedPelletWeightMax,
                UseCases = useCases,
                CompatiblePowerplants = compatiblePowerplants
            };
        }

        // ---------- Relationships ----------

        public async Task<List<MatchRelationship>> GetActiveRelationshipsForSourceAsync(string sourceShopifyProductId, string targetCategory)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var sql = "SELECT " + RelationshipColumns + " FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Source AND IsActive = 1" +
                          (targetCategory != null ? " AND TargetCategory = @TargetCategory" : "") + ";";
                var rows = (await connection.QueryAsync<MatchRelationship>(sql, new { Source = sourceShopifyProductId, TargetCategory = targetCategory })).ToList();
                if (rows.Count == 0)
                {
                    return rows;
                }

                var ids = rows.Select(r => r.Id).ToList();
                var useCaseRows = (await connection.QueryAsync<(int MatchRelationshipId, string UseCase)>(
                    "SELECT MatchRelationshipId, UseCase FROM dbo.MatchRelationshipUseCases WHERE MatchRelationshipId IN @Ids;", new { Ids = ids })).ToList();
                var useCasesById = useCaseRows.GroupBy(r => r.MatchRelationshipId).ToDictionary(g => g.Key, g => g.Select(r => r.UseCase).ToList());

                foreach (var r in rows)
                {
                    r.UseCases = useCasesById.TryGetValue(r.Id, out var uc) ? uc : new List<string>();
                }
                return rows;
            }
        }

        public async Task<MatchRelationship> GetRelationshipByIdAsync(int id)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var row = await connection.QueryFirstOrDefaultAsync<MatchRelationship>(
                    "SELECT " + RelationshipColumns + " FROM dbo.MatchRelationships WHERE Id = @Id;", new { Id = id });
                if (row == null)
                {
                    return null;
                }
                row.UseCases = (await connection.QueryAsync<string>(
                    "SELECT UseCase FROM dbo.MatchRelationshipUseCases WHERE MatchRelationshipId = @Id;", new { Id = id })).ToList();
                return row;
            }
        }

        /// <summary>Finds a relationship for this exact pair regardless of Status/IsActive — mirrors the reference system's upsert-by-pair semantics (Mongo find_one, no active filter).</summary>
        public async Task<int?> FindRelationshipIdByPairAsync(string sourceShopifyProductId, string targetShopifyProductId)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int?>(
                    "SELECT Id FROM dbo.MatchRelationships WHERE SourceShopifyProductId = @Source AND TargetShopifyProductId = @Target;",
                    new { Source = sourceShopifyProductId, Target = targetShopifyProductId });
            }
        }

        public async Task<int> InsertRelationshipAsync(MatchRelationship relationship)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.MatchRelationships
                        (SourceShopifyProductId, TargetShopifyProductId, TargetCategory, Status, Priority, Reason, AdminNotes, CalibreOverride, IsActive)
                      OUTPUT INSERTED.Id
                      VALUES
                        (@SourceShopifyProductId, @TargetShopifyProductId, @TargetCategory, @Status, @Priority, @Reason, @AdminNotes, @CalibreOverride, @IsActive);",
                    relationship);
            }
        }

        public async Task UpdateRelationshipAsync(MatchRelationship relationship)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    @"UPDATE dbo.MatchRelationships
                      SET TargetCategory = @TargetCategory, Status = @Status, Priority = @Priority,
                          Reason = @Reason, AdminNotes = @AdminNotes, CalibreOverride = @CalibreOverride,
                          IsActive = @IsActive, UpdatedAt = SYSUTCDATETIME()
                      WHERE Id = @Id;",
                    relationship);
            }
        }

        /// <summary>
        /// Narrow update used only by bulk-mark: touches TargetCategory/Status/
        /// Priority/IsActive only, leaving Reason/AdminNotes/CalibreOverride/
        /// UseCases untouched — mirrors the reference bulk_mark()'s $set
        /// payload exactly, which never overwrites those fields on an
        /// existing relationship.
        /// </summary>
        public async Task UpdateRelationshipForBulkAsync(int id, string targetCategory, string status, int? priority)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    @"UPDATE dbo.MatchRelationships
                      SET TargetCategory = @TargetCategory, Status = @Status, Priority = @Priority,
                          IsActive = 1, UpdatedAt = SYSUTCDATETIME()
                      WHERE Id = @Id;",
                    new { Id = id, TargetCategory = targetCategory, Status = status, Priority = priority });
            }
        }

        public async Task SetRelationshipActiveAsync(int id, bool isActive)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    "UPDATE dbo.MatchRelationships SET IsActive = @IsActive, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;",
                    new { Id = id, IsActive = isActive });
            }
        }

        /// <summary>Full replace of a relationship's use-case tags — mirrors the reference system's array-assignment semantics (not incremental add/remove).</summary>
        public async Task ReplaceRelationshipUseCasesAsync(int relationshipId, List<string> useCases)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.MatchRelationshipUseCases WHERE MatchRelationshipId = @Id;", new { Id = relationshipId });
                if (useCases != null && useCases.Count > 0)
                {
                    var rows = useCases.Distinct().Select(uc => new { Id = relationshipId, UseCase = uc });
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.MatchRelationshipUseCases (MatchRelationshipId, UseCase) VALUES (@Id, @UseCase);", rows);
                }
            }
        }
    }
}
