using System.Collections.Generic;
using System.Threading.Tasks;
using One77.Core.Match;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Match
{
    public class MatchValidationTests : MatchTestBase
    {
        private readonly ITestOutputHelper _output;
        public MatchValidationTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Upsert_ValidSourceAirgun_Succeeds()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun");
            var pellet = Gid("pellet");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var view = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                Assert.True(view.Id > 0);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Upsert_InvalidSourceCategory_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var notAnAirgun = Gid("pellet-as-source");
            var target = Gid("target");
            await InsertEnrichmentAsync(notAnAirgun, MatchCategory.Pellet, calibre: "4.5mm");
            await InsertEnrichmentAsync(target, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await Assert.ThrowsAsync<MatchValidationException>(() =>
                    admin.UpsertRelationshipAsync(notAnAirgun, target, MatchStatus.Compatible, null, null, null, null, false));
            }
            finally
            {
                await CleanupProductsAsync(notAnAirgun, target);
            }
        }

        [Fact]
        public async Task Upsert_InvalidTargetCategory_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun");
            var anotherAirgun = Gid("airgun-as-target");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(anotherAirgun, MatchCategory.Airgun, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await Assert.ThrowsAsync<MatchValidationException>(() =>
                    admin.UpsertRelationshipAsync(airgun, anotherAirgun, MatchStatus.Compatible, null, null, null, null, false));
            }
            finally
            {
                await CleanupProductsAsync(airgun, anotherAirgun);
            }
        }

        [Fact]
        public async Task Upsert_SelfMatch_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-self");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await Assert.ThrowsAsync<MatchValidationException>(() =>
                    admin.UpsertRelationshipAsync(airgun, airgun, MatchStatus.Compatible, null, null, null, null, false));
            }
            finally
            {
                await CleanupProductsAsync(airgun);
            }
        }

        [Fact]
        public async Task Upsert_CalibreMismatch_Recommended_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-45");
            var pellet = Gid("pellet-55");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "5.5mm");
            try
            {
                var admin = CreateAdminService();
                var ex = await Assert.ThrowsAsync<MatchValidationException>(() =>
                    admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, false));
                Assert.Contains("calibre", ex.Message.ToLowerInvariant());
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Upsert_CalibreMismatch_CompatibleStatus_NotBlocked()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // The mismatch guard only applies when Status = recommended.
            var airgun = Gid("airgun-45b");
            var pellet = Gid("pellet-55b");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "5.5mm");
            try
            {
                var admin = CreateAdminService();
                var view = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Compatible, null, null, null, null, false);
                Assert.True(view.Id > 0);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Upsert_CalibreOverride_AllowsMismatchedRecommended()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-45c");
            var pellet = Gid("pellet-55c");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "5.5mm");
            try
            {
                var admin = CreateAdminService();
                var view = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 3, null, null, null, calibreOverride: true);
                Assert.Equal(MatchStatus.Recommended, view.Status);
                Assert.True(view.CalibreOverride);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Upsert_MissingCalibreEitherSide_NoMismatchGuardTriggered()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // Reference behavior: missing calibre on either side means "no
            // mismatch detected" (guard passes) — no default applied here,
            // unlike the public derived resolver.
            var airgun = Gid("airgun-nocalibre");
            var pellet = Gid("pellet-nocalibre");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: null);
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "5.5mm");
            try
            {
                var admin = CreateAdminService();
                var view = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 1, null, null, null, false);
                Assert.Equal(MatchStatus.Recommended, view.Status);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Upsert_InvalidStatus_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-badstatus");
            var pellet = Gid("pellet-badstatus");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                await Assert.ThrowsAsync<MatchValidationException>(() =>
                    admin.UpsertRelationshipAsync(airgun, pellet, "maybe", null, null, null, null, false));
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Upsert_InvalidPriority_SilentlyCoercedToTwo_NotRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // Reference behavior: `priority = body.priority or 2; ... elif
            // priority not in (1,2,3): priority = 2` — an out-of-range value
            // is silently normalized, never rejected, on the upsert endpoint.
            var airgun = Gid("airgun-badpriority");
            var pellet = Gid("pellet-badpriority");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var view = await admin.UpsertRelationshipAsync(airgun, pellet, MatchStatus.Recommended, 99, null, null, null, false);
                Assert.Equal(2, view.Priority);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }

        [Fact]
        public async Task Upsert_UseCases_ArePersisted()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun-usecases");
            var pellet = Gid("pellet-usecases");
            await InsertEnrichmentAsync(airgun, MatchCategory.Airgun, calibre: "4.5mm");
            await InsertEnrichmentAsync(pellet, MatchCategory.Pellet, calibre: "4.5mm");
            try
            {
                var admin = CreateAdminService();
                var view = await admin.UpsertRelationshipAsync(
                    airgun, pellet, MatchStatus.Recommended, 1, new List<string> { "target_10m", "plinking" }, null, null, false);
                Assert.Equal(2, view.UseCases.Count);
                Assert.Contains("target_10m", view.UseCases);
                Assert.Contains("plinking", view.UseCases);
            }
            finally
            {
                await CleanupProductsAsync(airgun, pellet);
            }
        }
    }
}
