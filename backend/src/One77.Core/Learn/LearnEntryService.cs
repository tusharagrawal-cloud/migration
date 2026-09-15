using System.Collections.Generic;
using System.Threading.Tasks;

namespace One77.Core.Learn
{
    public sealed class LearnEntryService
    {
        private readonly LearnRepository _repository;

        public LearnEntryService(LearnRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<LearnEntryWithCategoryName>> GetAdminEntriesAsync(int? categoryId)
        {
            var entries = await _repository.GetAllEntriesAsync(categoryId);
            var names = await _repository.GetCategoryNamesAsync();

            var result = new List<LearnEntryWithCategoryName>();
            foreach (var e in entries)
            {
                string name;
                names.TryGetValue(e.CategoryId, out name);
                result.Add(new LearnEntryWithCategoryName
                {
                    Id = e.Id,
                    CategoryId = e.CategoryId,
                    Title = e.Title,
                    Body = e.Body,
                    SortPriority = e.SortPriority,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    CategoryName = name
                });
            }
            return result;
        }

        public async Task<LearnEntry> GetByIdAsync(int id)
        {
            var entry = await _repository.GetEntryByIdAsync(id);
            if (entry == null)
            {
                throw new LearnNotFoundException("Entry not found.");
            }
            return entry;
        }

        public async Task<LearnEntry> CreateAsync(int categoryId, string title, string body, int sortPriority, string status)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new LearnValidationException("Title is required.");
            }
            if (!LearnStatus.IsValid(status))
            {
                throw new LearnValidationException("Invalid status.");
            }
            if (!await _repository.CategoryExistsAsync(categoryId))
            {
                throw new LearnValidationException("The selected category could not be found.");
            }

            var id = await _repository.InsertEntryAsync(categoryId, title, body ?? "", sortPriority, status);
            return await _repository.GetEntryByIdAsync(id);
        }

        public async Task<LearnEntry> UpdateAsync(int id, int? categoryId, string title, string body, int? sortPriority, string status)
        {
            var current = await _repository.GetEntryByIdAsync(id);
            if (current == null)
            {
                throw new LearnNotFoundException("Entry not found.");
            }

            if (status != null && !LearnStatus.IsValid(status))
            {
                throw new LearnValidationException("Invalid status.");
            }

            if (categoryId.HasValue && !await _repository.CategoryExistsAsync(categoryId.Value))
            {
                throw new LearnValidationException("The selected category could not be found.");
            }

            var updated = new LearnEntry
            {
                Id = current.Id,
                CategoryId = categoryId.HasValue ? categoryId.Value : current.CategoryId,
                Title = !string.IsNullOrWhiteSpace(title) ? title : current.Title,
                Body = body != null ? body : current.Body,
                SortPriority = sortPriority.HasValue ? sortPriority.Value : current.SortPriority,
                Status = status != null ? status : current.Status
            };

            await _repository.UpdateEntryAsync(updated);
            return await _repository.GetEntryByIdAsync(id);
        }

        public async Task<LearnEntry> PublishAsync(int id)
        {
            await EnsureExistsAsync(id);
            await _repository.SetEntryStatusAsync(id, LearnStatus.Live);
            return await _repository.GetEntryByIdAsync(id);
        }

        public async Task<LearnEntry> ArchiveAsync(int id)
        {
            await EnsureExistsAsync(id);
            await _repository.SetEntryStatusAsync(id, LearnStatus.Hidden);
            return await _repository.GetEntryByIdAsync(id);
        }

        public async Task<LearnEntry> UnarchiveAsync(int id)
        {
            await EnsureExistsAsync(id);
            await _repository.SetEntryStatusAsync(id, LearnStatus.Draft);
            return await _repository.GetEntryByIdAsync(id);
        }

        private async Task EnsureExistsAsync(int id)
        {
            var entry = await _repository.GetEntryByIdAsync(id);
            if (entry == null)
            {
                throw new LearnNotFoundException("Entry not found.");
            }
        }
    }
}
