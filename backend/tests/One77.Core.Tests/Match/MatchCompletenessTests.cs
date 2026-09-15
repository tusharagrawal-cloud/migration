using System.Threading.Tasks;
using One77.Core.Match;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Match
{
    public class MatchCompletenessTests : MatchTestBase
    {
        private readonly ITestOutputHelper _output;
        public MatchCompletenessTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Completeness_NoMatches_WhenNoRelationships()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-completeness-none");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var completeness = await admin.GetCompletenessAsync(airgun);
                Assert.Equal("no_matches", completeness.State);
            }
            finally
            {
                await CleanupProductsAsync(airgun);
            }
        }

        [Fact]
        public async Task Completeness_NeedsReview_WhenOnlyAccessoryPresent()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-completeness-review");
            var accessory = Gid("accessory-completeness-review");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory);
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, accessory, MatchStatus.Compatible, null, null, null, null, false);

                var completeness = await admin.GetCompletenessAsync(airgun);
                Assert.Equal("needs_review", completeness.State);
            }
            finally
            {
                await CleanupProductsAsync(airgun, accessory);
            }
        }

        [Fact]
        public async Task Completeness_NeedsReview_WhenPelletsCompatibleButNoneRecommended()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // Pellets require >=1 compatible AND >=1 recommended — compatible alone is not enough.
            var airgun = Gid("airgun-completeness-pelletonly");
            var pellet = Gid("pellet-completeness-pelletonly");
            var accessory = Gid("accessory-completeness-pelletonly");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory);
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                await admin.UpsertRelationshipAsync(airgun, accessory, MatchStatus.Compatible, null, null, null, null, false);

                var completeness = await admin.GetCompletenessAsync(airgun);
                Assert.Equal("needs_review", completeness.State);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet, accessory);
            }
        }

        [Fact]
        public async Task Completeness_Complete_WhenPelletRecommendedAndAccessoryCompatible()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-completeness-complete");
            var pellet = Gid("pellet-completeness-complete");
            var accessory = Gid("accessory-completeness-complete");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory);
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, false);
                await admin.UpsertRelationshipAsync(airgun, accessory, MatchStatus.Compatible, null, null, null, null, false);

                var completeness = await admin.GetCompletenessAsync(airgun);
                Assert.Equal(1, completeness.CompatPellets);
                Assert.Equal(1, completeness.RecPellets);
                Assert.Equal(1, completeness.CompatAcc);
                Assert.Equal("complete", completeness.State);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet, accessory);
            }
        }

        [Fact]
        public async Task Completeness_NotRecommendedRelationships_DoNotCount()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-completeness-notrec");
            var pellet = Gid("pellet-completeness-notrec");
            var accessory = Gid("accessory-completeness-notrec");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(accessory, MatchCategory.Accessory);
            try
            {
                var admin = CreateAdminService();
                await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.NotRecommended, null, null, null, null, false);
                await admin.UpsertRelationshipAsync(airgun, accessory, MatchStatus.NotRecommended, null, null, null, null, false);

                var completeness = await admin.GetCompletenessAsync(airgun);
                Assert.Equal(0, completeness.CompatPellets);
                Assert.Equal(0, completeness.CompatAcc);
                Assert.Equal("no_matches", completeness.State);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet, accessory);
            }
        }
    }
}
