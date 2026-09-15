using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using One77.Core.Match;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Match
{
    /// <summary>
    /// Milestone 6, Section 19/20: representative parity fixtures A-S, each
    /// documenting INPUT / OLD EXPECTED BEHAVIOR (from the actual
    /// backend/match_routes.py + backend/models.py source, not from memory)
    /// / NEW ACTUAL BEHAVIOR / PASS-FAIL. Mirrored in
    /// migration/docs/MATCH_PARITY_REPORT.md.
    /// </summary>
    public class MatchParityFixtureTests : MatchTestBase
    {
        private readonly ITestOutputHelper _output;
        public MatchParityFixtureTests(ITestOutputHelper output) => _output = output;

        // A. Curated pellets only.
        // INPUT: airgun with one curated compatible pellet, no accessory relationships, no derivable accessory.
        // OLD: curated_pellets=True, curated_acc=False -> reason_source="curated" (curated wins if EITHER category has curated data); accessories fall back to derived (empty if none compatible).
        // NEW: same.
        [Fact]
        public async Task A_CuratedPelletsOnly()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureA");
            var pellet = Gid("pellet-fixtureA");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm", powerplantType: "springer");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Equal("curated", result.ReasonSource);
                Assert.Single(result.CompatiblePellets);
                Assert.True(result.CompatiblePellets[0].IsCurated);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        // B. Curated accessories only.
        // OLD: curated_acc=True, curated_pellets=False -> reason_source="curated"; pellets fall back to derived.
        // NEW: same.
        [Fact]
        public async Task B_CuratedAccessoriesOnly()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureB");
            var accessory = Gid("accessory-fixtureB");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm", powerplantType: "springer");
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory);
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, accessory, MatchStatus.Compatible, null, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Equal("curated", result.ReasonSource);
                Assert.Single(result.CompatibleAccessories);
                Assert.True(result.CompatibleAccessories[0].IsCurated);
            }
            finally
            {
                await CleanupProductsAsync(airgun, accessory);
            }
        }

        // C. Curated both categories.
        // OLD/NEW: both categories use curated data, reason_source="curated".
        [Fact]
        public async Task C_CuratedBoth()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureC");
            var pellet = Gid("pellet-fixtureC");
            var accessory = Gid("accessory-fixtureC");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm", powerplantType: "springer");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory);
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                await admin.UpsertRelationshipAsync(airgun, accessory, MatchStatus.Compatible, null, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Equal("curated", result.ReasonSource);
                Assert.True(result.CompatiblePellets[0].IsCurated);
                Assert.True(result.CompatibleAccessories[0].IsCurated);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet, accessory);
            }
        }

        // D. No curated pellets -> derived pellets.
        // OLD: derived rule = pellet.calibre==airgun.calibre AND pellet.use_case in airgun.match_use_cases AND weight in [min,max] (default 0-999); sorted by weight ascending.
        // NEW: same, using ProductUseCases (Milestone 6 schema correction) for the use-case gate.
        [Fact]
        public async Task D_NoCuratedPellets_DerivedPelletsUsed()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureD");
            var heavier = Gid("pellet-fixtureD-heavy");
            var lighter = Gid("pellet-fixtureD-light");
            var wrongUseCase = Gid("pellet-fixtureD-wronguc");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm", recommendedMin: 7m, recommendedMax: 10m, useCases: new[] { "target_10m" });
            await InsertEnrichmentAsync(heavier, MatchCategory.Pellet, calibre: "4.5mm", weightGrains: 9.5m, useCases: new[] { "target_10m" });
            await InsertEnrichmentAsync(lighter, MatchCategory.Pellet, calibre: "4.5mm", weightGrains: 8.0m, useCases: new[] { "target_10m" });
            await InsertEnrichmentAsync(wrongUseCase, MatchCategory.Pellet, calibre: "4.5mm", weightGrains: 8.5m, useCases: new[] { "plinking" });
            try
            {
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Equal("derived", result.ReasonSource);
                Assert.Equal(2, result.CompatiblePellets.Count);
                Assert.Equal(lighter, result.CompatiblePellets[0].Product.ShopifyProductId); // lighter first (weight ascending)
                Assert.Equal(heavier, result.CompatiblePellets[1].Product.ShopifyProductId);
                Assert.DoesNotContain(result.CompatiblePellets, c => c.Product.ShopifyProductId == wrongUseCase);
                Assert.False(result.CompatiblePellets[0].IsCurated);
            }
            finally
            {
                await CleanupProductsAsync(airgun, heavier, lighter, wrongUseCase);
            }
        }

        // E. No curated accessories -> derived accessories.
        // OLD: derived rule = airgun.powerplant (default "springer") in accessory.compatible_categories.
        // NEW: same, using ProductCompatiblePowerplants.
        [Fact]
        public async Task E_NoCuratedAccessories_DerivedAccessoriesUsed()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureE");
            var compatible = Gid("accessory-fixtureE-compat");
            var incompatible = Gid("accessory-fixtureE-incompat");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm", powerplantType: "pcp");
            await InsertEnrichmentAsync(compatible, MatchCategory.Accessory, compatiblePowerplants: new[] { "pcp", "springer" });
            await InsertEnrichmentAsync(incompatible, MatchCategory.Accessory, compatiblePowerplants: new[] { "co2" });
            try
            {
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Equal("derived", result.ReasonSource);
                Assert.Contains(result.CompatibleAccessories, c => c.Product.ShopifyProductId == compatible);
                Assert.DoesNotContain(result.CompatibleAccessories, c => c.Product.ShopifyProductId == incompatible);
                Assert.False(result.CompatibleAccessories[0].IsCurated);
            }
            finally
            {
                await CleanupProductsAsync(airgun, compatible, incompatible);
            }
        }

        // F. Curated pellets + derived accessories (independent per category).
        // OLD: reason_source="curated" (>=1 category curated) even though accessories used the derived branch internally.
        // NEW: same — this is the case that proves reason_source is response-level, not per-category.
        [Fact]
        public async Task F_CuratedPelletsPlusDerivedAccessories()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureF");
            var pellet = Gid("pellet-fixtureF");
            var accessory = Gid("accessory-fixtureF");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm", powerplantType: "pcp");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory, compatiblePowerplants: new[] { "pcp" });
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Equal("curated", result.ReasonSource); // response-level flag, not per-category
                Assert.True(result.CompatiblePellets[0].IsCurated);
                Assert.False(result.CompatibleAccessories[0].IsCurated); // accessory branch is still derived internally
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet, accessory);
            }
        }

        // G. Derived pellets + curated accessories.
        // OLD/NEW: same response-level reason_source="curated" rule as F, mirrored the other way.
        [Fact]
        public async Task G_DerivedPelletsPlusCuratedAccessories()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureG");
            var pellet = Gid("pellet-fixtureG");
            var accessory = Gid("accessory-fixtureG");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm", powerplantType: "pcp", useCases: new[] { "plinking" });
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm", weightGrains: 8m, useCases: new[] { "plinking" });
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory);
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, accessory, MatchStatus.Compatible, null, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Equal("curated", result.ReasonSource);
                Assert.False(result.CompatiblePellets[0].IsCurated); // pellet branch is derived internally
                Assert.True(result.CompatibleAccessories[0].IsCurated);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet, accessory);
            }
        }

        // H. not_recommended target excluded from public response entirely.
        // OLD: query excludes status=not_recommended at the source ({"$ne": STATUS_NOT}).
        // NEW: same (WHERE Status != 'not_recommended' equivalent, via C# filter on active relationships).
        [Fact]
        public async Task H_NotRecommendedExcluded()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureH");
            var pellet = Gid("pellet-fixtureH");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.NotRecommended, null, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.DoesNotContain(result.Pellets, p => p.Product.ShopifyProductId == pellet);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        // I/J/K. recommended priority 1/2/3.
        // OLD: priority 1 -> best_match bucket, label "Best Match"; 2 and 3 -> recommended bucket, labels "Recommended"/"Alternative" respectively.
        // NEW: same (see MatchPublicResolverTests for the combined 2+3-in-one-bucket assertion).
        [Fact]
        public async Task I_RecommendedPriority1_BestMatch()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureI");
            var pellet = Gid("pellet-fixtureI");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Single(result.BestMatchPellets);
                Assert.Equal("Best Match", result.BestMatchPellets[0].PriorityLabel);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task J_RecommendedPriority2_Recommended()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureJ");
            var pellet = Gid("pellet-fixtureJ");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 2, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Single(result.RecommendedPellets);
                Assert.Equal("Recommended", result.RecommendedPellets[0].PriorityLabel);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task K_RecommendedPriority3_Alternative()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureK");
            var pellet = Gid("pellet-fixtureK");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 3, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Single(result.RecommendedPellets);
                Assert.Equal("Alternative", result.RecommendedPellets[0].PriorityLabel);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        // L. compatible.
        // OLD/NEW: status="compatible" -> compatible bucket, no priority/label.
        [Fact]
        public async Task L_Compatible()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureL");
            var pellet = Gid("pellet-fixtureL");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                var result = await CreateResolver().ResolveAsync(airgun, null);

                Assert.Single(result.CompatiblePellets);
                Assert.Null(result.CompatiblePellets[0].Priority);
                Assert.Equal("", result.CompatiblePellets[0].PriorityLabel);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        // M. calibre mismatch blocked.
        // OLD: recommended + mismatched calibre + no override -> HTTP 400 "Calibre does not match...".
        // NEW: MatchValidationException with the same message (see MatchValidationTests.Upsert_CalibreMismatch_Recommended_Rejected for the full assertion).
        [Fact]
        public async Task M_CalibreMismatchBlocked()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureM");
            var pellet = Gid("pellet-fixtureM");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "5.5mm");
            try
            {
                await Assert.ThrowsAsync<MatchValidationException>(() =>
                    CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, false));
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        // N. calibre_override accepted.
        // OLD/NEW: same mismatch, calibre_override=true -> saved successfully.
        [Fact]
        public async Task N_CalibreOverrideAccepted()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureN");
            var pellet = Gid("pellet-fixtureN");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "5.5mm");
            try
            {
                var view = await CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, calibreOverride: true);
                Assert.True(view.Id > 0);
                Assert.True(view.CalibreOverride);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        // O. soft-delete relationship.
        // OLD: DELETE sets active=false, row persists; disappears from public/admin-active views.
        // NEW: same (see MatchAdminCrudTests.Delete_SoftDeactivates_RowStillExists for the full assertion).
        [Fact]
        public async Task O_SoftDeleteRelationship()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureO");
            var pellet = Gid("pellet-fixtureO");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var created = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                await admin.DeleteRelationshipAsync(created.Id);

                var resultAfter = await CreateResolver().ResolveAsync(airgun, null);
                Assert.DoesNotContain(resultAfter.Pellets, p => p.Product.ShopifyProductId == pellet);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        // P. remove + re-add.
        // OLD: soft-delete then upsert on the SAME pair reactivates/overwrites the SAME document (Mongo find_one has no active filter) — never creates a second row.
        // NEW: same, via FindRelationshipIdByPairAsync (any status) + update-in-place.
        [Fact]
        public async Task P_RemoveThenReAdd_ReactivatesSameRow()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureP");
            var pellet = Gid("pellet-fixtureP");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var created = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                await admin.DeleteRelationshipAsync(created.Id);

                var readded = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, false);

                Assert.Equal(created.Id, readded.Id); // same row reactivated, not a new one
                Assert.True(readded.IsActive);

                var resultAfter = await CreateResolver().ResolveAsync(airgun, null);
                Assert.Contains(resultAfter.Pellets, p => p.Product.ShopifyProductId == pellet);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        // Q/R/S. completeness = complete / needs_review / no_matches.
        // OLD: pellets need >=1 compatible AND >=1 recommended; accessories need only >=1 compatible; see MatchCompletenessTests for the full state-machine coverage.
        [Fact]
        public async Task Q_Completeness_Complete()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureQ");
            var pellet = Gid("pellet-fixtureQ");
            var accessory = Gid("accessory-fixtureQ");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory);
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, false);
                await admin.UpsertRelationshipAsync(airgun, accessory, MatchStatus.Compatible, null, null, null, null, false);
                Assert.Equal("complete", (await admin.GetCompletenessAsync(airgun)).State);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet, accessory);
            }
        }

        [Fact]
        public async Task R_Completeness_NeedsReview()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureR");
            var pellet = Gid("pellet-fixtureR");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                await CreateAdminService().UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                Assert.Equal("needs_review", (await CreateAdminService().GetCompletenessAsync(airgun)).State);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task S_Completeness_NoMatches()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-fixtureS");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            try
            {
                Assert.Equal("no_matches", (await CreateAdminService().GetCompletenessAsync(airgun)).State);
            }
            finally
            {
                await CleanupProductsAsync(airgun);
            }
        }
    }
}
