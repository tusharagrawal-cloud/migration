using System.Linq;
using System.Threading.Tasks;
using One77.Core.Learn;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Learn
{
    public class AdminEntryLearnTests : LearnTestBase
    {
        private readonly ITestOutputHelper _output;
        public AdminEntryLearnTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Create_Succeeds()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var category = await categories.CreateAsync(Unique("Entry Create Topic"), null, null, 0, LearnStatus.Draft);
            try
            {
                var entry = await entries.CreateAsync(category.Id, "New Entry", "Body text", 2, LearnStatus.Draft);
                Assert.True(entry.Id > 0);
                Assert.Equal(category.Id, entry.CategoryId);
                Assert.Equal(2, entry.SortPriority);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task Create_RejectsNonexistentCategory()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var entries = CreateEntryService();
            await Assert.ThrowsAsync<LearnValidationException>(() =>
                entries.CreateAsync(-999999, "Orphan Entry", "Body", 0, LearnStatus.Draft));
        }

        [Fact]
        public async Task Create_RejectsInvalidStatus()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var category = await categories.CreateAsync(Unique("Bad Entry Status Topic"), null, null, 0, LearnStatus.Draft);
            try
            {
                await Assert.ThrowsAsync<LearnValidationException>(() =>
                    entries.CreateAsync(category.Id, "Bad Status Entry", "Body", 0, "Archived"));
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task Update_UpdatesFields()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var category = await categories.CreateAsync(Unique("Entry Update Topic"), null, null, 0, LearnStatus.Draft);
            var entry = await entries.CreateAsync(category.Id, "Original Title", "Original Body", 0, LearnStatus.Draft);
            try
            {
                var updated = await entries.UpdateAsync(entry.Id, null, "Updated Title", "Updated Body", 7, null);
                Assert.Equal("Updated Title", updated.Title);
                Assert.Equal("Updated Body", updated.Body);
                Assert.Equal(7, updated.SortPriority);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task Update_RejectsNonexistentCategory()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var category = await categories.CreateAsync(Unique("Entry Recategorize Topic"), null, null, 0, LearnStatus.Draft);
            var entry = await entries.CreateAsync(category.Id, "Movable Entry", "Body", 0, LearnStatus.Draft);
            try
            {
                await Assert.ThrowsAsync<LearnValidationException>(() =>
                    entries.UpdateAsync(entry.Id, -999999, null, null, null, null));
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task GetById_MissingReturns404()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var entries = CreateEntryService();
            await Assert.ThrowsAsync<LearnNotFoundException>(() => entries.GetByIdAsync(-999999));
        }

        [Fact]
        public async Task Publish_SetsLive()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var category = await categories.CreateAsync(Unique("Entry Publish Topic"), null, null, 0, LearnStatus.Live);
            var entry = await entries.CreateAsync(category.Id, "Publish Entry", "Body", 0, LearnStatus.Draft);
            try
            {
                var published = await entries.PublishAsync(entry.Id);
                Assert.Equal(LearnStatus.Live, published.Status);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task Archive_SetsHidden()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var category = await categories.CreateAsync(Unique("Entry Archive Topic"), null, null, 0, LearnStatus.Live);
            var entry = await entries.CreateAsync(category.Id, "Archive Entry", "Body", 0, LearnStatus.Live);
            try
            {
                var archived = await entries.ArchiveAsync(entry.Id);
                Assert.Equal(LearnStatus.Hidden, archived.Status);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task Unarchive_SetsDraft()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var category = await categories.CreateAsync(Unique("Entry Unarchive Topic"), null, null, 0, LearnStatus.Live);
            var entry = await entries.CreateAsync(category.Id, "Unarchive Entry", "Body", 0, LearnStatus.Hidden);
            try
            {
                var unarchived = await entries.UnarchiveAsync(entry.Id);
                Assert.Equal(LearnStatus.Draft, unarchived.Status);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task List_CategoryFilterWorks()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var categoryA = await categories.CreateAsync(Unique("Filter Topic A"), null, null, 0, LearnStatus.Draft);
            var categoryB = await categories.CreateAsync(Unique("Filter Topic B"), null, null, 0, LearnStatus.Draft);
            var entryA = await entries.CreateAsync(categoryA.Id, "Entry In A", "Body", 0, LearnStatus.Draft);
            var entryB = await entries.CreateAsync(categoryB.Id, "Entry In B", "Body", 0, LearnStatus.Draft);
            try
            {
                var filtered = await entries.GetAdminEntriesAsync(categoryA.Id);
                Assert.Contains(filtered, e => e.Id == entryA.Id);
                Assert.DoesNotContain(filtered, e => e.Id == entryB.Id);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(categoryA.Id);
                await DeleteCategoryCascadeAsync(categoryB.Id);
            }
        }

        [Fact]
        public async Task List_CategoryNameIsCorrect()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var categoryName = Unique("Named Topic");
            var category = await categories.CreateAsync(categoryName, null, null, 0, LearnStatus.Draft);
            var entry = await entries.CreateAsync(category.Id, "Entry With Category Name", "Body", 0, LearnStatus.Draft);
            try
            {
                var all = await entries.GetAdminEntriesAsync(null);
                var found = all.Single(e => e.Id == entry.Id);
                Assert.Equal(categoryName, found.CategoryName);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }
    }
}
