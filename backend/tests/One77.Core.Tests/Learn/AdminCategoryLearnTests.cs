using System.Linq;
using System.Threading.Tasks;
using One77.Core.Learn;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Learn
{
    public class AdminCategoryLearnTests : LearnTestBase
    {
        private readonly ITestOutputHelper _output;
        public AdminCategoryLearnTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Create_Succeeds()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var created = await categories.CreateAsync(Unique("New Topic"), null, "desc", 3, LearnStatus.Draft);
            try
            {
                Assert.True(created.Id > 0);
                Assert.Equal(LearnStatus.Draft, created.Status);
                Assert.Equal(3, created.SortPriority);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(created.Id);
            }
        }

        [Fact]
        public async Task Create_AutoGeneratesSlugFromName()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var suffix = Unique("x");
            var created = await categories.CreateAsync($"Airgun Basics {suffix}", null, null, 0, LearnStatus.Draft);
            try
            {
                Assert.Equal(SlugHelper.Slugify($"Airgun Basics {suffix}"), created.Slug);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(created.Id);
            }
        }

        [Fact]
        public async Task Create_DuplicateSlugGetsSuffixed()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var slug = Unique("shared-slug");
            var first = await categories.CreateAsync("First Name", slug, null, 0, LearnStatus.Draft);
            var second = await categories.CreateAsync("Second Name", slug, null, 0, LearnStatus.Draft);
            try
            {
                Assert.Equal(slug, first.Slug);
                Assert.Equal(slug + "-2", second.Slug);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(first.Id);
                await DeleteCategoryCascadeAsync(second.Id);
            }
        }

        [Fact]
        public async Task Create_InvalidStatusRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            await Assert.ThrowsAsync<LearnValidationException>(() =>
                categories.CreateAsync(Unique("Bad Status Topic"), null, null, 0, "Published"));
        }

        [Fact]
        public async Task Update_UpdatesFields()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var created = await categories.CreateAsync(Unique("Original Name"), null, "orig desc", 0, LearnStatus.Draft);
            try
            {
                var updated = await categories.UpdateAsync(created.Id, "Updated Name", null, "updated desc", 9, null);
                Assert.Equal("Updated Name", updated.Name);
                Assert.Equal("updated desc", updated.Description);
                Assert.Equal(9, updated.SortPriority);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(created.Id);
            }
        }

        [Fact]
        public async Task Update_NameChangeAlone_DoesNotRegenerateSlug()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var originalSlug = Unique("stable-slug");
            var created = await categories.CreateAsync("Original Name", originalSlug, null, 0, LearnStatus.Draft);
            try
            {
                var updated = await categories.UpdateAsync(created.Id, "A Totally Different Name", null, null, null, null);
                Assert.Equal(originalSlug, updated.Slug);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(created.Id);
            }
        }

        [Fact]
        public async Task Update_InvalidStatusRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var created = await categories.CreateAsync(Unique("Status Update Topic"), null, null, 0, LearnStatus.Draft);
            try
            {
                await Assert.ThrowsAsync<LearnValidationException>(() =>
                    categories.UpdateAsync(created.Id, null, null, null, null, "Archived"));
            }
            finally
            {
                await DeleteCategoryCascadeAsync(created.Id);
            }
        }

        [Fact]
        public async Task GetById_MissingReturns404()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            await Assert.ThrowsAsync<LearnNotFoundException>(() => categories.GetByIdAsync(-999999));
        }

        [Fact]
        public async Task Publish_SetsLive()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var created = await categories.CreateAsync(Unique("Publish Topic"), null, null, 0, LearnStatus.Draft);
            try
            {
                var published = await categories.PublishAsync(created.Id);
                Assert.Equal(LearnStatus.Live, published.Status);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(created.Id);
            }
        }

        [Fact]
        public async Task Archive_SetsHidden()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var created = await categories.CreateAsync(Unique("Archive Topic"), null, null, 0, LearnStatus.Live);
            try
            {
                var archived = await categories.ArchiveAsync(created.Id);
                Assert.Equal(LearnStatus.Hidden, archived.Status);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(created.Id);
            }
        }

        [Fact]
        public async Task Unarchive_SetsDraft()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var created = await categories.CreateAsync(Unique("Unarchive Topic"), null, null, 0, LearnStatus.Hidden);
            try
            {
                var unarchived = await categories.UnarchiveAsync(created.Id);
                Assert.Equal(LearnStatus.Draft, unarchived.Status);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(created.Id);
            }
        }

        [Fact]
        public async Task List_IncludesAllStatuses()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var draft = await categories.CreateAsync(Unique("List Draft"), null, null, 0, LearnStatus.Draft);
            var live = await categories.CreateAsync(Unique("List Live"), null, null, 0, LearnStatus.Live);
            var hidden = await categories.CreateAsync(Unique("List Hidden"), null, null, 0, LearnStatus.Hidden);
            try
            {
                var all = await categories.GetAdminCategoriesAsync();
                Assert.Contains(all, c => c.Id == draft.Id);
                Assert.Contains(all, c => c.Id == live.Id);
                Assert.Contains(all, c => c.Id == hidden.Id);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(draft.Id);
                await DeleteCategoryCascadeAsync(live.Id);
                await DeleteCategoryCascadeAsync(hidden.Id);
            }
        }

        [Fact]
        public async Task List_EntryCountIsCorrect()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var created = await categories.CreateAsync(Unique("Count Topic"), null, null, 0, LearnStatus.Draft);
            var e1 = await entries.CreateAsync(created.Id, "Entry 1", "Body", 0, LearnStatus.Draft);
            var e2 = await entries.CreateAsync(created.Id, "Entry 2", "Body", 0, LearnStatus.Live);
            try
            {
                var all = await categories.GetAdminCategoriesAsync();
                var found = all.Single(c => c.Id == created.Id);
                Assert.Equal(2, found.EntryCount);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(created.Id);
            }
        }
    }
}
