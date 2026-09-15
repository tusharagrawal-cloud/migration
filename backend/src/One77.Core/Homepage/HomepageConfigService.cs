using System.Threading.Tasks;

namespace One77.Core.Homepage
{
    /// <summary>
    /// Business rules for the homepage's one admin-controlled content field:
    /// the hero photograph. Replacing an existing photograph deletes the old
    /// file so uploads never accumulate; removing it clears the reference so
    /// the storefront's own fallback hero (dark gradient + decorative mark)
    /// takes over automatically — that fallback is entirely frontend-owned,
    /// nothing here tells it what to render.
    /// </summary>
    public sealed class HomepageConfigService
    {
        private readonly HomepageConfigRepository _repository;
        private readonly HomepageMediaStorage _storage;

        public HomepageConfigService(HomepageConfigRepository repository, HomepageMediaStorage storage)
        {
            _repository = repository;
            _storage = storage;
        }

        public Task<HomepageConfig> GetAsync() => _repository.GetAsync();

        public async Task<HomepageConfig> SetHeroImageAsync(byte[] content)
        {
            var current = await _repository.GetAsync();
            var newPath = _storage.SaveHeroImage(content);

            await _repository.SetHeroImagePathAsync(newPath);

            if (!string.IsNullOrWhiteSpace(current.HeroImagePath))
            {
                _storage.Delete(current.HeroImagePath);
            }

            return await _repository.GetAsync();
        }

        public async Task<HomepageConfig> RemoveHeroImageAsync()
        {
            var current = await _repository.GetAsync();

            await _repository.SetHeroImagePathAsync(null);

            if (!string.IsNullOrWhiteSpace(current.HeroImagePath))
            {
                _storage.Delete(current.HeroImagePath);
            }

            return await _repository.GetAsync();
        }
    }
}
