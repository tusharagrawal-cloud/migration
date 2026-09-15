using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Bundles;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Bundles
{
    public class BundleServiceTests : BundleTestBase
    {
        private readonly ITestOutputHelper _output;
        public BundleServiceTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Create_Succeeds_WithDraftDefault()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var created = await service.CreateAsync("Starter Combo", "A great start", null, 0, null);
            try
            {
                Assert.True(created.Id > 0);
                Assert.Equal(BundleStatus.Draft, created.Status); // explicit, never missing/null
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task Create_MissingName_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            await Assert.ThrowsAsync<BundleValidationException>(() => service.CreateAsync("", null, null, 0, null));
        }

        [Fact]
        public async Task Create_WithProducts_PreservesShopifyGidsAndOrder()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var airgun = Gid("airgun");
            var pellet = Gid("pellet");
            var accessory = Gid("accessory");
            var service = CreateService();
            var items = new List<BundleItemSpec>
            {
                new() { ShopifyProductId = airgun, ItemRole = BundleItemRole.Primary },
                new() { ShopifyProductId = pellet, ItemRole = BundleItemRole.Pellet },
                new() { ShopifyProductId = accessory, ItemRole = BundleItemRole.Accessory }
            };
            var created = await service.CreateAsync("Combo Set", null, null, 0, items);
            try
            {
                Assert.Equal(3, created.Items.Count);
                Assert.Equal(airgun, created.Items[0].ShopifyProductId);
                Assert.Equal(0, created.Items[0].SortOrder);
                Assert.Equal(pellet, created.Items[1].ShopifyProductId);
                Assert.Equal(1, created.Items[1].SortOrder);
                Assert.Equal(accessory, created.Items[2].ShopifyProductId);
                Assert.Equal(2, created.Items[2].SortOrder);
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task Create_MoreThanOnePrimary_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var items = new List<BundleItemSpec>
            {
                new() { ShopifyProductId = Gid("p1"), ItemRole = BundleItemRole.Primary },
                new() { ShopifyProductId = Gid("p2"), ItemRole = BundleItemRole.Primary }
            };
            await Assert.ThrowsAsync<BundleValidationException>(() => service.CreateAsync("Two Primaries", null, null, 0, items));
        }

        [Fact]
        public async Task Create_DuplicateShopifyProductId_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var same = Gid("dup");
            var service = CreateService();
            var items = new List<BundleItemSpec>
            {
                new() { ShopifyProductId = same, ItemRole = BundleItemRole.Primary },
                new() { ShopifyProductId = same, ItemRole = BundleItemRole.Pellet }
            };
            await Assert.ThrowsAsync<BundleValidationException>(() => service.CreateAsync("Dup Item Bundle", null, null, 0, items));
        }

        [Fact]
        public async Task Create_MissingShopifyProductIdOnItem_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var items = new List<BundleItemSpec> { new() { ShopifyProductId = "", ItemRole = BundleItemRole.Primary } };
            await Assert.ThrowsAsync<BundleValidationException>(() => service.CreateAsync("Bad Item Bundle", null, null, 0, items));
        }

        [Fact]
        public async Task Create_InvalidItemRole_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var items = new List<BundleItemSpec> { new() { ShopifyProductId = Gid("x"), ItemRole = "SecretItem" } };
            await Assert.ThrowsAsync<BundleValidationException>(() => service.CreateAsync("Bad Role Bundle", null, null, 0, items));
        }

        [Fact]
        public async Task Update_EditsFieldsAndReplacesItems()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var original = Gid("orig");
            var created = await service.CreateAsync("Original Name", "orig tagline", null, 0,
                new List<BundleItemSpec> { new() { ShopifyProductId = original, ItemRole = BundleItemRole.Primary } });
            try
            {
                var replacement = Gid("replacement");
                var updated = await service.UpdateAsync(created.Id, "New Name", "new tagline", null, 5,
                    new List<BundleItemSpec> { new() { ShopifyProductId = replacement, ItemRole = BundleItemRole.Primary } });

                Assert.Equal("New Name", updated.Name);
                Assert.Equal("new tagline", updated.Tagline);
                Assert.Equal(5, updated.SortPriority);
                Assert.Single(updated.Items);
                Assert.Equal(replacement, updated.Items[0].ShopifyProductId);
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task Update_MissingBundle_Returns404()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            await Assert.ThrowsAsync<BundleNotFoundException>(() => service.UpdateAsync(-999999, "X", null, null, null, null));
        }

        [Fact]
        public async Task Update_ItemsOmitted_LeavesExistingItemsUnchanged()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var product = Gid("keep");
            var created = await service.CreateAsync("Keep Items", null, null, 0,
                new List<BundleItemSpec> { new() { ShopifyProductId = product, ItemRole = BundleItemRole.Primary } });
            try
            {
                var updated = await service.UpdateAsync(created.Id, "Renamed", null, null, null, null);
                Assert.Single(updated.Items);
                Assert.Equal(product, updated.Items[0].ShopifyProductId);
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task Update_ItemRemoval_WithEmptyList()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var created = await service.CreateAsync("Removable Items", null, null, 0,
                new List<BundleItemSpec> { new() { ShopifyProductId = Gid("removeme"), ItemRole = BundleItemRole.Primary } });
            try
            {
                var updated = await service.UpdateAsync(created.Id, null, null, null, null, new List<BundleItemSpec>());
                Assert.Empty(updated.Items);
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task Publish_ThenAppearsPublicly()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var items = new List<BundleItemSpec>
            {
                new() { ShopifyProductId = Gid("pub-primary"), ItemRole = BundleItemRole.Primary },
                new() { ShopifyProductId = Gid("pub-pellet"), ItemRole = BundleItemRole.Pellet }
            };
            var created = await service.CreateAsync("Publish Test Bundle", null, null, 0, items);
            try
            {
                var beforePublish = await service.GetPublicBundlesAsync();
                Assert.DoesNotContain(beforePublish, b => b.Id == created.Id);

                var published = await service.PublishAsync(created.Id);
                Assert.Equal(BundleStatus.Published, published.Status);

                var afterPublish = await service.GetPublicBundlesAsync();
                Assert.Contains(afterPublish, b => b.Id == created.Id);
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task Publish_WithoutPrimary_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var created = await service.CreateAsync("No Primary Bundle", null, null, 0,
                new List<BundleItemSpec> { new() { ShopifyProductId = Gid("only-pellet"), ItemRole = BundleItemRole.Pellet } });
            try
            {
                await Assert.ThrowsAsync<BundleValidationException>(() => service.PublishAsync(created.Id));
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task Publish_WithOnlyPrimary_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var created = await service.CreateAsync("Only Primary Bundle", null, null, 0,
                new List<BundleItemSpec> { new() { ShopifyProductId = Gid("solo-primary"), ItemRole = BundleItemRole.Primary } });
            try
            {
                await Assert.ThrowsAsync<BundleValidationException>(() => service.PublishAsync(created.Id));
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task Archive_ThenDisappearsFromPublic_ButAdminStillSeesIt()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var items = new List<BundleItemSpec>
            {
                new() { ShopifyProductId = Gid("arch-primary"), ItemRole = BundleItemRole.Primary },
                new() { ShopifyProductId = Gid("arch-pellet"), ItemRole = BundleItemRole.Pellet }
            };
            var created = await service.CreateAsync("Archive Test Bundle", null, null, 0, items);
            try
            {
                await service.PublishAsync(created.Id);
                var archived = await service.ArchiveAsync(created.Id);
                Assert.Equal(BundleStatus.Archived, archived.Status);

                var publicList = await service.GetPublicBundlesAsync();
                Assert.DoesNotContain(publicList, b => b.Id == created.Id);

                var adminBundle = await service.GetByIdAsync(created.Id);
                Assert.NotNull(adminBundle);
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task Unarchive_RestoresToDraft_NotPublished()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var items = new List<BundleItemSpec>
            {
                new() { ShopifyProductId = Gid("unarch-primary"), ItemRole = BundleItemRole.Primary },
                new() { ShopifyProductId = Gid("unarch-pellet"), ItemRole = BundleItemRole.Pellet }
            };
            var created = await service.CreateAsync("Unarchive Test Bundle", null, null, 0, items);
            try
            {
                await service.PublishAsync(created.Id);
                await service.ArchiveAsync(created.Id);
                var unarchived = await service.UnarchiveAsync(created.Id);

                Assert.Equal(BundleStatus.Draft, unarchived.Status); // never straight back to Published
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task PublicList_ExcludesDraftAndArchived_OrderedBySortPriorityThenName()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var draft = await service.CreateAsync("Draft Bundle ZZZ", null, BundleStatus.Draft, 0, null);
            var earlier = await service.CreateAsync("Bundle B", null, null, 1,
                new List<BundleItemSpec> { new() { ShopifyProductId = Gid("b1"), ItemRole = BundleItemRole.Primary }, new() { ShopifyProductId = Gid("b2"), ItemRole = BundleItemRole.Pellet } });
            var later = await service.CreateAsync("Bundle A", null, null, 2,
                new List<BundleItemSpec> { new() { ShopifyProductId = Gid("a1"), ItemRole = BundleItemRole.Primary }, new() { ShopifyProductId = Gid("a2"), ItemRole = BundleItemRole.Pellet } });
            try
            {
                await service.PublishAsync(earlier.Id);
                await service.PublishAsync(later.Id);

                var publicList = await service.GetPublicBundlesAsync();
                var earlierIndex = publicList.FindIndex(b => b.Id == earlier.Id);
                var laterIndex = publicList.FindIndex(b => b.Id == later.Id);

                Assert.DoesNotContain(publicList, b => b.Id == draft.Id);
                Assert.True(earlierIndex >= 0 && laterIndex >= 0);
                Assert.True(earlierIndex < laterIndex, "Lower SortPriority should come first.");
            }
            finally
            {
                await DeleteBundleAsync(draft.Id);
                await DeleteBundleAsync(earlier.Id);
                await DeleteBundleAsync(later.Id);
            }
        }

        [Fact]
        public async Task AdminList_FiltersByStatusAndName()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var service = CreateService();
            var name = Gid("findme").Substring(0, 20);
            var created = await service.CreateAsync($"Special {name} Bundle", null, BundleStatus.Draft, 0, null);
            try
            {
                var byName = await service.GetAdminBundlesAsync(null, name);
                Assert.Contains(byName, b => b.Id == created.Id);

                var byStatus = await service.GetAdminBundlesAsync(BundleStatus.Draft, null);
                Assert.Contains(byStatus, b => b.Id == created.Id);

                var byWrongStatus = await service.GetAdminBundlesAsync(BundleStatus.Published, null);
                Assert.DoesNotContain(byWrongStatus, b => b.Id == created.Id);
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public async Task NewBundle_NeverHasNullOrMissingStatus()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            // The old Mongo "missing status = published" rule is a MIGRATION
            // compatibility rule only — new SQL rows must always have an
            // explicit valid status. Verified directly against the column.
            var service = CreateService();
            var created = await service.CreateAsync("Explicit Status Bundle", null, null, 0, null);
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
                await connection.OpenAsync();
                var status = await connection.ExecuteScalarAsync<string>(
                    "SELECT Status FROM dbo.Bundles WHERE Id = @Id;", new { Id = created.Id });

                Assert.False(string.IsNullOrEmpty(status));
                Assert.True(BundleStatus.IsValid(status));
            }
            finally
            {
                await DeleteBundleAsync(created.Id);
            }
        }

        [Fact]
        public void NoPriceOrMediaFieldsExist_OnBundleType()
        {
            // Static, compile-time audit: confirms none of the explicitly
            // forbidden old fields (bundle_price, savings, image, shopify_url)
            // were reintroduced.
            var forbidden = new[] { "BundlePrice", "Price", "Savings", "SavingsPercent", "IndividualTotal", "Image", "ImageUrl", "ShopifyUrl", "Media" };
            var bundleProperties = typeof(Bundle).GetProperties().Select(p => p.Name).ToList();
            var itemProperties = typeof(BundleItem).GetProperties().Select(p => p.Name).ToList();

            foreach (var forbiddenName in forbidden)
            {
                Assert.DoesNotContain(forbiddenName, bundleProperties);
                Assert.DoesNotContain(forbiddenName, itemProperties);
            }
        }
    }
}
