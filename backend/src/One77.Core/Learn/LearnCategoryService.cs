using System.Collections.Generic;
using System.Threading.Tasks;

namespace One77.Core.Learn
{
    public sealed class LearnCategoryService
    {
        private readonly LearnRepository _repository;

        public LearnCategoryService(LearnRepository repository)
        {
            _repository = repository;
        }

        public Task<List<LearnCategory>> GetPublicCategoriesAsync()
        {
            return _repository.GetLiveCategoriesAsync();
        }

        public async Task<LearnCategoryDetail> GetPublicCategoryDetailAsync(string slug)
        {
            var category = await _repository.GetLiveCategoryBySlugAsync(slug);
            if (category == null)
            {
                throw new LearnNotFoundException("Topic not found.");
            }

            var entries = await _repository.GetLiveEntriesForCategoryAsync(category.Id);
            return new LearnCategoryDetail { Category = category, Entries = entries };
        }

        public async Task<List<LearnCategoryWithCount>> GetAdminCategoriesAsync()
        {
            var categories = await _repository.GetAllCategoriesAsync();
            var counts = await _repository.GetEntryCountsByCategoryAsync();

            var result = new List<LearnCategoryWithCount>();
            foreach (var c in categories)
            {
                int count;
                counts.TryGetValue(c.Id, out count);
                result.Add(new LearnCategoryWithCount
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    SortPriority = c.SortPriority,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    EntryCount = count
                });
            }
            return result;
        }

        public async Task<LearnCategory> GetByIdAsync(int id)
        {
            var category = await _repository.GetCategoryByIdAsync(id);
            if (category == null)
            {
                throw new LearnNotFoundException("Category not found.");
            }
            return category;
        }

        public async Task<LearnCategory> CreateAsync(string name, string slug, string description, int sortPriority, string status)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new LearnValidationException("Name is required.");
            }
            if (!LearnStatus.IsValid(status))
            {
                throw new LearnValidationException("Invalid status.");
            }

            var slugSeed = !string.IsNullOrWhiteSpace(slug) ? slug : name;
            var uniqueSlug = await EnsureUniqueSlugAsync(SlugHelper.Slugify(slugSeed), null);

            var id = await _repository.InsertCategoryAsync(name, uniqueSlug, description ?? "", sortPriority, status);
            return await _repository.GetCategoryByIdAsync(id);
        }

        public async Task<LearnCategory> UpdateAsync(int id, string name, string slug, string description, int? sortPriority, string status)
        {
            var current = await _repository.GetCategoryByIdAsync(id);
            if (current == null)
            {
                throw new LearnNotFoundException("Category not found.");
            }

            if (status != null && !LearnStatus.IsValid(status))
            {
                throw new LearnValidationException("Invalid status.");
            }

            var updated = new LearnCategory
            {
                Id = current.Id,
                Name = !string.IsNullOrWhiteSpace(name) ? name : current.Name,
                Slug = current.Slug,
                Description = description != null ? description : current.Description,
                SortPriority = sortPriority.HasValue ? sortPriority.Value : current.SortPriority,
                Status = status != null ? status : current.Status
            };

            // Slug is only ever regenerated when the caller explicitly asks to
            // change it — a Name change alone must not silently rewrite an
            // existing, possibly externally-linked slug.
            if (!string.IsNullOrWhiteSpace(slug))
            {
                updated.Slug = await EnsureUniqueSlugAsync(SlugHelper.Slugify(slug), id);
            }

            await _repository.UpdateCategoryAsync(updated);
            return await _repository.GetCategoryByIdAsync(id);
        }

        public async Task<LearnCategory> PublishAsync(int id)
        {
            await EnsureExistsAsync(id);
            await _repository.SetCategoryStatusAsync(id, LearnStatus.Live);
            return await _repository.GetCategoryByIdAsync(id);
        }

        public async Task<LearnCategory> ArchiveAsync(int id)
        {
            await EnsureExistsAsync(id);
            await _repository.SetCategoryStatusAsync(id, LearnStatus.Hidden);
            return await _repository.GetCategoryByIdAsync(id);
        }

        public async Task<LearnCategory> UnarchiveAsync(int id)
        {
            await EnsureExistsAsync(id);
            await _repository.SetCategoryStatusAsync(id, LearnStatus.Draft);
            return await _repository.GetCategoryByIdAsync(id);
        }

        private async Task EnsureExistsAsync(int id)
        {
            var category = await _repository.GetCategoryByIdAsync(id);
            if (category == null)
            {
                throw new LearnNotFoundException("Category not found.");
            }
        }

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? excludeId)
        {
            var root = string.IsNullOrEmpty(baseSlug) ? "topic" : baseSlug;
            var candidate = root;
            var n = 2;
            while (await _repository.CategorySlugExistsAsync(candidate, excludeId))
            {
                candidate = root + "-" + n;
                n++;
            }
            return candidate;
        }
    }
}
