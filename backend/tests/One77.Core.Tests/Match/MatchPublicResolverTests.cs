using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using One77.Core.Match;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Match
{
    public class MatchPublicResolverTests : MatchTestBase
    {
        private readonly ITestOutputHelper _output;
        public MatchPublicResolverTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Resolve_MissingAirgun_ThrowsNotFound()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var resolver = CreateResolver();
            await Assert.ThrowsAsync<MatchNotFoundException>(() => resolver.ResolveAsync(Gid("nonexistent"), null));
        }

        [Fact]
        public async Task Resolve_SourceNotAnAirgun_ThrowsNotFound()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var pellet = Gid("pellet-as-resolve-source");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var resolver = CreateResolver();
                await Assert.ThrowsAsync<MatchNotFoundException>(() => resolver.ResolveAsync(pellet, null));
            }
            finally
            {
                await CleanupProductsAsync(pellet);
            }
        }

        [Fact]
        public async Task Resolve_PreservesShopifyProductIdOnSourceAndTargets()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-gidpreserve");
            var pellet = Gid("pellet-gidpreserve");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);

                var resolver = CreateResolver();
                var result = await resolver.ResolveAsync(airgun, null);

                Assert.Equal(airgun, result.Source.ShopifyProductId);
                Assert.Contains(result.Pellets, p => p.Product.ShopifyProductId == pellet);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Resolve_Priority1_GoesToBestMatchBucket_WithLabel()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-p1");
            var pellet = Gid("pellet-p1");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, false);

                var result = await CreateResolver().ResolveAsync(airgun, null);
                Assert.Single(result.BestMatchPellets);
                Assert.Equal("Best Match", result.BestMatchPellets[0].PriorityLabel);
                Assert.Empty(result.RecommendedPellets);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Resolve_Priority2And3_BothGoToRecommendedBucket_WithDistinctLabels()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // Only two response buckets exist for recommended: best_match
            // (priority 1) and recommended (priority 2 AND 3 combined) — there
            // is no separate "alternative" bucket in the response shape.
            var airgun = Gid("airgun-p2p3");
            var pelletP2 = Gid("pellet-p2");
            var pelletP3 = Gid("pellet-p3");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pelletP2, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(pelletP3, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pelletP2, MatchStatus.Recommended, 2, null, null, null, false);
                await admin.UpsertRelationshipAsync(airgun, pelletP3, MatchStatus.Recommended, 3, null, null, null, false);

                var result = await CreateResolver().ResolveAsync(airgun, null);
                Assert.Empty(result.BestMatchPellets);
                Assert.Equal(2, result.RecommendedPellets.Count);
                Assert.Equal(pelletP2, result.RecommendedPellets[0].Product.ShopifyProductId);
                Assert.Equal("Recommended", result.RecommendedPellets[0].PriorityLabel);
                Assert.Equal(pelletP3, result.RecommendedPellets[1].Product.ShopifyProductId);
                Assert.Equal("Alternative", result.RecommendedPellets[1].PriorityLabel);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pelletP2, pelletP3);
            }
        }

        [Fact]
        public async Task Resolve_Compatible_GoesToCompatibleBucket()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-compat");
            var pellet = Gid("pellet-compat");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);

                var result = await CreateResolver().ResolveAsync(airgun, null);
                Assert.Single(result.CompatiblePellets);
                Assert.Equal("compatible", result.CompatiblePellets[0].Status);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Resolve_NotRecommended_NeverAppearsPublicly()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-notrec-public");
            var pellet = Gid("pellet-notrec-public");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.NotRecommended, null, null, null, null, false);

                var result = await CreateResolver().ResolveAsync(airgun, null);
                Assert.DoesNotContain(result.Pellets, p => p.Product.ShopifyProductId == pellet);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Resolve_InactiveTarget_ExcludedFromPublicResponse_ButStillActiveRelationship()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // ONE77-owned substitute for the reference system's Shopify
            // lifecycle target filter (status=published, active=true).
            var airgun = Gid("airgun-inactivetarget");
            var pellet = Gid("pellet-inactivetarget");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm", isActive: false);
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);

                var result = await CreateResolver().ResolveAsync(airgun, null);
                Assert.DoesNotContain(result.Pellets, p => p.Product.ShopifyProductId == pellet);

                var rels = await admin.GetRelationshipsAsync(airgun, null);
                Assert.Contains(rels, r => r.TargetShopifyProductId == pellet);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Resolve_UseCaseFilter_DropsCurratedItemsRestrictedToOtherUseCases()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-ucfilter");
            var matching = Gid("pellet-uc-matching");
            var otherUseCase = Gid("pellet-uc-other");
            var general = Gid("pellet-uc-general");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(matching, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(otherUseCase, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(general, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, matching, MatchStatus.Compatible, null, new List<string> { "target_10m" }, null, null, false);
                await admin.UpsertRelationshipAsync(airgun, otherUseCase, MatchStatus.Compatible, null, new List<string> { "plinking" }, null, null, false);
                await admin.UpsertRelationshipAsync(airgun, general, MatchStatus.Compatible, null, null, null, null, false);

                var result = await CreateResolver().ResolveAsync(airgun, "target_10m");
                var ids = result.Pellets.Select(p => p.Product.ShopifyProductId).ToList();

                Assert.Contains(matching, ids);
                Assert.Contains(general, ids);
                Assert.DoesNotContain(otherUseCase, ids);
            }
            finally
            {
                await CleanupProductsAsync(airgun, matching, otherUseCase, general);
            }
        }
    }
}
