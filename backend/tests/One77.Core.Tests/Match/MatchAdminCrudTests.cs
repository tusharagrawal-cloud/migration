using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Match;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Match
{
    public class MatchAdminCrudTests : MatchTestBase
    {
        private readonly ITestOutputHelper _output;
        public MatchAdminCrudTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Upsert_SamePairTwice_UpdatesSameRow_DoesNotDuplicate()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // Reference behavior (Mongo find_one by pair, no active filter):
            // a second upsert on the same (source, target) pair updates the
            // SAME row in place rather than creating a new one.
            var airgun = Gid("airgun-idempotent");
            var pellet = Gid("pellet-idempotent");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var first = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, new List<string> { "target_10m" }, "first", null, false);
                var second = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 2, new List<string> { "target_10m", "plinking" }, "second", null, false);

                Assert.Equal(first.Id, second.Id);
                Assert.Equal(2, second.Priority);
                Assert.Equal("second", second.Reason);
                Assert.Equal(2, second.UseCases.Count);

                var rels = await admin.GetRelationshipsAsync(airgun, null);
                Assert.Single(rels.Where(r => r.TargetShopifyProductId == pellet));
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task GetRelationships_FiltersByTargetCategory()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-relfilter");
            var pellet = Gid("pellet-relfilter");
            var accessory = Gid("accessory-relfilter");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm", powerplantType: "springer");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory);
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                await admin.UpsertRelationshipAsync(airgun, accessory, MatchStatus.Compatible, null, null, null, null, false);

                var pelletsOnly = await admin.GetRelationshipsAsync(airgun, MatchCategory.Pellet);
                Assert.Single(pelletsOnly);
                Assert.Equal(pellet, pelletsOnly[0].TargetShopifyProductId);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet, accessory);
            }
        }

        [Fact]
        public async Task GetCandidates_ReturnsCategoryMatches_WithCurrentRelationshipAttached()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-candidates");
            var linkedPellet = Gid("pellet-linked");
            var unlinkedPellet = Gid("pellet-unlinked");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(linkedPellet, MatchCategory.Pellet, calibre: "4.5mm", weightGrains: 8.0m);
            await InsertEnrichmentAsync(unlinkedPellet, MatchCategory.Pellet, calibre: "4.5mm", weightGrains: 9.0m);
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, linkedPellet, MatchStatus.Compatible, null, null, null, null, false);

                var candidates = await admin.GetCandidatesAsync(airgun, MatchCategory.Pellet, null, null);
                var linked = candidates.Single(c => c.Product.ShopifyProductId == linkedPellet);
                var unlinked = candidates.Single(c => c.Product.ShopifyProductId == unlinkedPellet);

                Assert.NotNull(linked.CurrentRelationship);
                Assert.Null(unlinked.CurrentRelationship);
            }
            finally
            {
                await CleanupProductsAsync(airgun, linkedPellet, unlinkedPellet);
            }
        }

        [Fact]
        public async Task GetCandidates_FiltersByCalibreAndWeight()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-candfilter");
            var close = Gid("pellet-close");
            var far = Gid("pellet-far");
            var wrongCalibre = Gid("pellet-wrongcal");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(close, MatchCategory.Pellet, calibre: "4.5mm", weightGrains: 8.0m);
            await InsertEnrichmentAsync(far, MatchCategory.Pellet, calibre: "4.5mm", weightGrains: 20.0m);
            await InsertEnrichmentAsync(wrongCalibre, MatchCategory.Pellet, calibre: "5.5mm", weightGrains: 8.0m);
            try
            {
                var admin = CreateAdminService();
                var candidates = await admin.GetCandidatesAsync(airgun, MatchCategory.Pellet, "4.5mm", 8.0m);

                Assert.Contains(candidates, c => c.Product.ShopifyProductId == close);
                Assert.DoesNotContain(candidates, c => c.Product.ShopifyProductId == far);
                Assert.DoesNotContain(candidates, c => c.Product.ShopifyProductId == wrongCalibre);
            }
            finally
            {
                await CleanupProductsAsync(airgun, close, far, wrongCalibre);
            }
        }

        [Fact]
        public async Task Patch_UpdatesStatusAndPriority()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-patch");
            var pellet = Gid("pellet-patch");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var created = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);

                var patched = await admin.PatchRelationshipAsync(created.Id, MatchStatus.Recommended, 1, null, null, null, null, null);
                Assert.Equal(MatchStatus.Recommended, patched.Status);
                Assert.Equal(1, patched.Priority);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Patch_StatusChangedAwayFromRecommended_ForcesPriorityNull()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-patchnull");
            var pellet = Gid("pellet-patchnull");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var created = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, false);

                var patched = await admin.PatchRelationshipAsync(created.Id, MatchStatus.Compatible, null, null, null, null, null, null);
                Assert.Equal(MatchStatus.Compatible, patched.Status);
                Assert.Null(patched.Priority);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Patch_InvalidStatus_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-patchbadstatus");
            var pellet = Gid("pellet-patchbadstatus");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var created = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);

                await Assert.ThrowsAsync<MatchValidationException>(() =>
                    admin.PatchRelationshipAsync(created.Id, "bogus", null, null, null, null, null, null));
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Patch_PriorityWithoutRecommendedStatus_RejectedWithCleanError()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // Documented improvement over the reference system: the old PATCH
            // endpoint does not validate Priority unless Status is part of the
            // same call, which would otherwise reach our DB's CHECK constraint
            // as a raw, unhandled 500. See MATCH_PARITY_REPORT.md.
            var airgun = Gid("airgun-patchpriorityguard");
            var pellet = Gid("pellet-patchpriorityguard");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var created = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);

                await Assert.ThrowsAsync<MatchValidationException>(() =>
                    admin.PatchRelationshipAsync(created.Id, null, 2, null, null, null, null, null));
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Patch_MissingRelationship_Returns404()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var admin = CreateAdminService();
            await Assert.ThrowsAsync<MatchNotFoundException>(() =>
                admin.PatchRelationshipAsync(-999999, MatchStatus.Compatible, null, null, null, null, null, null));
        }

        [Fact]
        public async Task Delete_SoftDeactivates_RowStillExists()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-delete");
            var pellet = Gid("pellet-delete");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var created = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);

                await admin.DeleteRelationshipAsync(created.Id);

                var afterDelete = await admin.GetRelationshipsAsync(airgun, null);
                Assert.DoesNotContain(afterDelete, r => r.TargetShopifyProductId == pellet);

                using var connection = await OpenRawAsync();
                var isActive = await connection.ExecuteScalarAsync<bool>(
                    "SELECT IsActive FROM dbo.MatchRelationships WHERE Id = @Id;", new { Id = created.Id });
                Assert.False(isActive);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Delete_MissingRelationship_Returns404()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var admin = CreateAdminService();
            await Assert.ThrowsAsync<MatchNotFoundException>(() => admin.DeleteRelationshipAsync(-999999));
        }

        [Fact]
        public async Task Bulk_CreatesThenUpdates_Idempotently()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-bulk");
            var pelletA = Gid("pellet-bulk-a");
            var pelletB = Gid("pellet-bulk-b");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pelletA, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(pelletB, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var first = await admin.BulkMarkAsync(airgun, new List<string> { pelletA, pelletB }, MatchStatus.Compatible);
                Assert.Equal(2, first.Created);
                Assert.Equal(0, first.Updated);

                var second = await admin.BulkMarkAsync(airgun, new List<string> { pelletA, pelletB }, MatchStatus.Compatible);
                Assert.Equal(0, second.Created);
                Assert.Equal(2, second.Updated);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pelletA, pelletB);
            }
        }

        [Fact]
        public async Task Bulk_SkipsCalibreMismatch_ForRecommended()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-bulkskip");
            var mismatchedPellet = Gid("pellet-bulkskip");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(mismatchedPellet, MatchCategory.Pellet, calibre: "5.5mm");
            try
            {
                var admin = CreateAdminService();
                var result = await admin.BulkMarkAsync(airgun, new List<string> { mismatchedPellet }, MatchStatus.Recommended);
                Assert.Equal(0, result.Created);
                Assert.Equal(1, result.SkippedCalibre);
            }
            finally
            {
                await CleanupProductsAsync(airgun, mismatchedPellet);
            }
        }

        [Fact]
        public async Task Bulk_PreservesReasonAndUseCases_OnUpdate()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // Documented parity detail: the reference bulk endpoint's $set
            // payload never touches reason/admin_notes/use_cases on an
            // existing relationship — only source/target/category/status/
            // priority/active. Preserved exactly.
            var airgun = Gid("airgun-bulkpreserve");
            var pellet = Gid("pellet-bulkpreserve");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, new List<string> { "target_10m" }, "curated reason", "internal note", false);

                await admin.BulkMarkAsync(airgun, new List<string> { pellet }, MatchStatus.Compatible);

                var rels = await admin.GetRelationshipsAsync(airgun, null);
                var rel = rels.Single(r => r.TargetShopifyProductId == pellet);
                Assert.Equal("curated reason", rel.Reason);
                Assert.Equal("internal note", rel.AdminNotes);
                Assert.Contains("target_10m", rel.UseCases);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Bulk_SourceNeedNotBeAirgun_MatchesReferenceLooseness()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // Documented asymmetry: unlike single upsert, bulk_mark only
            // requires the source product to exist — not that it be an
            // airgun. Preserved exactly.
            var notAnAirgun = Gid("pellet-as-bulk-source");
            var target = Gid("target-of-loose-bulk");
            await InsertEnrichmentAsync(notAnAirgun, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(target, MatchCategory.Accessory);
            try
            {
                var admin = CreateAdminService();
                var result = await admin.BulkMarkAsync(notAnAirgun, new List<string> { target }, MatchStatus.Compatible);
                Assert.Equal(1, result.Created);
            }
            finally
            {
                await CleanupProductsAsync(notAnAirgun, target);
            }
        }

        [Fact]
        public async Task Airguns_List_IncludesCompleteness()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-list");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm", powerplantType: "springer");
            try
            {
                var admin = CreateAdminService();
                var airguns = await admin.GetAirgunsAsync();
                var found = airguns.Single(a => a.ShopifyProductId == airgun);
                Assert.Equal("no_matches", found.Counts.State);
            }
            finally
            {
                await CleanupProductsAsync(airgun);
            }
        }
    }
}
