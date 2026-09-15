using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using One77.Core.Enrichment;
using One77.Core.Match;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Enrichment
{
    public class EnrichmentServiceTests : EnrichmentTestBase
    {
        private readonly ITestOutputHelper _output;
        public EnrichmentServiceTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Create_Succeeds_AndGidSurvivesUnchanged()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("airgun");
            try
            {
                var service = CreateService();
                var created = await service.CreateAsync(gid, MatchCategory.Airgun, true, "4.5mm", "springer", null, 7m, 10m, null, null, null);

                Assert.True(created.Id > 0);
                Assert.Equal(gid, created.ShopifyProductId); // opaque, byte-for-byte, never parsed
                Assert.Equal(MatchCategory.Airgun, created.Category);
                Assert.True(created.IsActive);
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task Create_DuplicateShopifyProductId_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("dup");
            try
            {
                var service = CreateService();
                await service.CreateAsync(gid, MatchCategory.Pellet, true, "4.5mm", null, 8m, null, null, null, null, null);

                await Assert.ThrowsAsync<EnrichmentValidationException>(() =>
                    service.CreateAsync(gid, MatchCategory.Pellet, true, "5.5mm", null, 9m, null, null, null, null, null));
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task Create_MissingShopifyProductId_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            await Assert.ThrowsAsync<EnrichmentValidationException>(() =>
                service.CreateAsync("", MatchCategory.Pellet, true, null, null, null, null, null, null, null, null));
        }

        [Fact]
        public async Task Create_InvalidCategory_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("badcat");
            var service = CreateService();
            await Assert.ThrowsAsync<EnrichmentValidationException>(() =>
                service.CreateAsync(gid, "spaceship", true, null, null, null, null, null, null, null, null));
        }

        [Fact]
        public async Task GetByShopifyProductId_RetrievesRecord()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("retrieve");
            try
            {
                var service = CreateService();
                await service.CreateAsync(gid, MatchCategory.Accessory, true, null, null, null, null, null, null, null, null);

                var fetched = await service.GetByShopifyProductIdAsync(gid);
                Assert.Equal(gid, fetched.ShopifyProductId);
                Assert.Equal(MatchCategory.Accessory, fetched.Category);
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task GetByShopifyProductId_MissingReturns404()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            await Assert.ThrowsAsync<EnrichmentNotFoundException>(() => service.GetByShopifyProductIdAsync(Gid("missing")));
        }

        [Fact]
        public async Task Update_UpdatesCoreFields()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("update");
            try
            {
                var service = CreateService();
                await service.CreateAsync(gid, MatchCategory.Airgun, true, "4.5mm", "springer", null, null, null, null, null, null);

                var updated = await service.UpdateAsync(gid, null, false, "5.5mm", "pcp", null, 8m, 12m, null, null, null);

                Assert.Equal("5.5mm", updated.Calibre);
                Assert.Equal("pcp", updated.PowerplantType);
                Assert.False(updated.IsActive);
                Assert.Equal(8m, updated.RecommendedPelletWeightMin);
                Assert.Equal(12m, updated.RecommendedPelletWeightMax);
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task Update_MissingRecord_Returns404()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            await Assert.ThrowsAsync<EnrichmentNotFoundException>(() =>
                service.UpdateAsync(Gid("missing-update"), null, null, null, null, null, null, null, null, null, null));
        }

        [Fact]
        public async Task Update_InvalidCategory_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("update-badcat");
            try
            {
                var service = CreateService();
                await service.CreateAsync(gid, MatchCategory.Airgun, true, null, null, null, null, null, null, null, null);

                await Assert.ThrowsAsync<EnrichmentValidationException>(() =>
                    service.UpdateAsync(gid, "spaceship", null, null, null, null, null, null, null, null, null));
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task Specifications_SaveReadUpdate()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("specs");
            try
            {
                var service = CreateService();
                var specs = new List<EnrichmentSpecification>
                {
                    new() { SpecKey = "barrel_type", SpecValue = "Rifled steel", SortOrder = 0 },
                    new() { SpecKey = "sights", SpecValue = "Open sights", SortOrder = 0 },
                    new() { SpecKey = "sights", SpecValue = "Fibre optic", SortOrder = 1 }
                };
                var created = await service.CreateAsync(gid, MatchCategory.Airgun, true, null, null, null, null, null, null, null, specs);

                Assert.Equal(3, created.Specifications.Count);
                Assert.Equal(2, created.Specifications.Count(s => s.SpecKey == "sights"));

                var updatedSpecs = new List<EnrichmentSpecification> { new() { SpecKey = "barrel_length", SpecValue = "19 inch", SortOrder = 0 } };
                var updated = await service.UpdateAsync(gid, null, null, null, null, null, null, null, null, null, updatedSpecs);

                Assert.Single(updated.Specifications);
                Assert.Equal("barrel_length", updated.Specifications[0].SpecKey);
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task Specifications_CanBeCleared_WithEmptyList()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("specs-clear");
            try
            {
                var service = CreateService();
                var specs = new List<EnrichmentSpecification> { new() { SpecKey = "material", SpecValue = "Aluminium", SortOrder = 0 } };
                await service.CreateAsync(gid, MatchCategory.Accessory, true, null, null, null, null, null, null, null, specs);

                var updated = await service.UpdateAsync(gid, null, null, null, null, null, null, null, null, null, new List<EnrichmentSpecification>());
                Assert.Empty(updated.Specifications);
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task CompatiblePowerplants_SaveReadUpdate()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("powerplants");
            try
            {
                var service = CreateService();
                var created = await service.CreateAsync(gid, MatchCategory.Accessory, true, null, null, null, null, null,
                    new List<string> { "springer", "pcp" }, null, null);

                Assert.Equal(2, created.CompatiblePowerplants.Count);

                var updated = await service.UpdateAsync(gid, null, null, null, null, null, null, null,
                    new List<string> { "co2" }, null, null);

                Assert.Single(updated.CompatiblePowerplants);
                Assert.Equal("co2", updated.CompatiblePowerplants[0]);
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task UseCases_SaveReadUpdate()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("usecases");
            try
            {
                var service = CreateService();
                var created = await service.CreateAsync(gid, MatchCategory.Airgun, true, null, null, null, null, null,
                    null, new List<string> { "target_10m", "plinking" }, null);

                Assert.Equal(2, created.UseCases.Count);

                var updated = await service.UpdateAsync(gid, null, null, null, null, null, null, null,
                    null, new List<string> { "plinking" }, null);

                Assert.Single(updated.UseCases);
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task GetAll_ReturnsRecordsWithChildren()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("list");
            try
            {
                var service = CreateService();
                await service.CreateAsync(gid, MatchCategory.Airgun, true, "4.5mm", "springer", null, null, null,
                    null, new List<string> { "plinking" }, new List<EnrichmentSpecification> { new() { SpecKey = "k", SpecValue = "v", SortOrder = 0 } });

                var all = await service.GetAllAsync();
                var found = all.Single(e => e.ShopifyProductId == gid);
                Assert.Single(found.UseCases);
                Assert.Single(found.Specifications);
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task PublicRead_ExcludesInactiveRecord()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("inactive-public");
            try
            {
                var service = CreateService();
                await service.CreateAsync(gid, MatchCategory.Pellet, false, "4.5mm", null, 8m, null, null, null, null, null);

                await Assert.ThrowsAsync<EnrichmentNotFoundException>(() => service.GetPublicByShopifyProductIdAsync(gid));
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public async Task PublicRead_ReturnsActiveRecord()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var gid = Gid("active-public");
            try
            {
                var service = CreateService();
                await service.CreateAsync(gid, MatchCategory.Pellet, true, "4.5mm", null, 8m, null, null, null, null, null);

                var record = await service.GetPublicByShopifyProductIdAsync(gid);
                Assert.Equal(gid, record.ShopifyProductId);
            }
            finally
            {
                await CleanupAsync(gid);
            }
        }

        [Fact]
        public void NoCommerceFieldsExist_OnEnrichmentRecordType()
        {
            // Static, compile-time audit: confirms no Shopify commerce field
            // (name, brand, price, compare-at price, inventory, image,
            // handle, description) was ever added to the enrichment shape.
            var forbidden = new[] { "Name", "Brand", "Vendor", "Price", "CompareAtPrice", "Inventory", "Image", "ImageUrl", "Handle", "Slug", "Description", "Title" };
            var actualProperties = typeof(EnrichmentRecord).GetProperties().Select(p => p.Name).ToList();

            foreach (var forbiddenName in forbidden)
            {
                Assert.DoesNotContain(forbiddenName, actualProperties);
            }
        }
    }
}
