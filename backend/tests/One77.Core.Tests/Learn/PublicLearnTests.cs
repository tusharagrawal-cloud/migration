using System.Linq;
using System.Threading.Tasks;
using One77.Core.Learn;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Learn
{
    public class PublicLearnTests : LearnTestBase
    {
        private readonly ITestOutputHelper _output;
        public PublicLearnTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Categories_ReturnsOnlyLiveCategories()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var live = await categories.CreateAsync(Unique("Live Topic"), null, null, 0, LearnStatus.Live);
            var draft = await categories.CreateAsync(Unique("Draft Topic"), null, null, 0, LearnStatus.Draft);
            try
            {
                var result = await categories.GetPublicCategoriesAsync();
                Assert.Contains(result, c => c.Id == live.Id);
                Assert.DoesNotContain(result, c => c.Id == draft.Id);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(live.Id);
                await DeleteCategoryCascadeAsync(draft.Id);
            }
        }

        [Fact]
        public async Task Categories_DraftCategoryExcluded()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var draft = await categories.CreateAsync(Unique("Draft Only"), null, null, 0, LearnStatus.Draft);
            try
            {
                var result = await categories.GetPublicCategoriesAsync();
                Assert.DoesNotContain(result, c => c.Id == draft.Id);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(draft.Id);
            }
        }

        [Fact]
        public async Task Categories_HiddenCategoryExcluded()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var hidden = await categories.CreateAsync(Unique("Hidden Only"), null, null, 0, LearnStatus.Hidden);
            try
            {
                var result = await categories.GetPublicCategoriesAsync();
                Assert.DoesNotContain(result, c => c.Id == hidden.Id);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(hidden.Id);
            }
        }

        [Fact]
        public async Task Categories_OrderedBySortPriorityThenName()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var prefix = Unique("Order");
            var second = await categories.CreateAsync($"{prefix} B", null, null, 5, LearnStatus.Live);
            var first = await categories.CreateAsync($"{prefix} A", null, null, 5, LearnStatus.Live);
            var earliest = await categories.CreateAsync($"{prefix} Z", null, null, 1, LearnStatus.Live);
            try
            {
                var result = await categories.GetPublicCategoriesAsync();
                var earliestIndex = result.FindIndex(c => c.Id == earliest.Id);
                var firstIndex = result.FindIndex(c => c.Id == first.Id);
                var secondIndex = result.FindIndex(c => c.Id == second.Id);

                Assert.True(earliestIndex < firstIndex, "Lower SortPriority (1) should come before SortPriority 5 group.");
                Assert.True(firstIndex < secondIndex, "Within equal SortPriority, Name 'A' should come before 'B'.");
            }
            finally
            {
                await DeleteCategoryCascadeAsync(second.Id);
                await DeleteCategoryCascadeAsync(first.Id);
                await DeleteCategoryCascadeAsync(earliest.Id);
            }
        }

        [Fact]
        public async Task CategoryDetail_ReturnsCategoryAndLiveEntries()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var slug = Unique("detail-topic");
            var category = await categories.CreateAsync("Detail Topic", slug, "A description", 0, LearnStatus.Live);
            var entry = await entries.CreateAsync(category.Id, "Live Entry", "Body text", 0, LearnStatus.Live);
            try
            {
                var detail = await categories.GetPublicCategoryDetailAsync(slug);
                Assert.Equal(category.Id, detail.Category.Id);
                Assert.Equal("Detail Topic", detail.Category.Name);
                Assert.Single(detail.Entries);
                Assert.Equal(entry.Id, detail.Entries[0].Id);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task CategoryDetail_DraftEntriesExcluded()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var slug = Unique("draft-entry-topic");
            var category = await categories.CreateAsync("Draft Entry Topic", slug, null, 0, LearnStatus.Live);
            var live = await entries.CreateAsync(category.Id, "Live Entry", "Body", 0, LearnStatus.Live);
            var draft = await entries.CreateAsync(category.Id, "Draft Entry", "Body", 0, LearnStatus.Draft);
            try
            {
                var detail = await categories.GetPublicCategoryDetailAsync(slug);
                Assert.Contains(detail.Entries, e => e.Id == live.Id);
                Assert.DoesNotContain(detail.Entries, e => e.Id == draft.Id);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task CategoryDetail_HiddenEntriesExcluded()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var slug = Unique("hidden-entry-topic");
            var category = await categories.CreateAsync("Hidden Entry Topic", slug, null, 0, LearnStatus.Live);
            var live = await entries.CreateAsync(category.Id, "Live Entry", "Body", 0, LearnStatus.Live);
            var hidden = await entries.CreateAsync(category.Id, "Hidden Entry", "Body", 0, LearnStatus.Hidden);
            try
            {
                var detail = await categories.GetPublicCategoryDetailAsync(slug);
                Assert.Contains(detail.Entries, e => e.Id == live.Id);
                Assert.DoesNotContain(detail.Entries, e => e.Id == hidden.Id);
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task CategoryDetail_LiveEntryUnderNonLiveCategory_NotAccessible()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var slug = Unique("draft-category-live-entry");
            var category = await categories.CreateAsync("Draft Category", slug, null, 0, LearnStatus.Draft);
            var entry = await entries.CreateAsync(category.Id, "Live Entry", "Body", 0, LearnStatus.Live);
            try
            {
                await Assert.ThrowsAsync<LearnNotFoundException>(() => categories.GetPublicCategoryDetailAsync(slug));
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task CategoryDetail_MissingSlug_ThrowsNotFound()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            await Assert.ThrowsAsync<LearnNotFoundException>(() => categories.GetPublicCategoryDetailAsync(Unique("nonexistent-slug")));
        }

        [Fact]
        public async Task CategoryDetail_NonLiveCategorySlug_ThrowsNotFound()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var slug = Unique("hidden-category");
            var category = await categories.CreateAsync("Hidden Category", slug, null, 0, LearnStatus.Hidden);
            try
            {
                await Assert.ThrowsAsync<LearnNotFoundException>(() => categories.GetPublicCategoryDetailAsync(slug));
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }

        [Fact]
        public async Task CategoryDetail_EntryOrderingIsSortPriorityThenTitle()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var categories = CreateCategoryService();
            var entries = CreateEntryService();
            var slug = Unique("entry-order-topic");
            var category = await categories.CreateAsync("Entry Order Topic", slug, null, 0, LearnStatus.Live);
            var second = await entries.CreateAsync(category.Id, "B Entry", "Body", 5, LearnStatus.Live);
            var first = await entries.CreateAsync(category.Id, "A Entry", "Body", 5, LearnStatus.Live);
            var earliest = await entries.CreateAsync(category.Id, "Z Entry", "Body", 1, LearnStatus.Live);
            try
            {
                var detail = await categories.GetPublicCategoryDetailAsync(slug);
                var earliestIndex = detail.Entries.FindIndex(e => e.Id == earliest.Id);
                var firstIndex = detail.Entries.FindIndex(e => e.Id == first.Id);
                var secondIndex = detail.Entries.FindIndex(e => e.Id == second.Id);

                Assert.True(earliestIndex < firstIndex, "Lower SortPriority (1) should come before SortPriority 5 group.");
                Assert.True(firstIndex < secondIndex, "Within equal SortPriority, Title 'A Entry' should come before 'B Entry'.");
            }
            finally
            {
                await DeleteCategoryCascadeAsync(category.Id);
            }
        }
    }
}
