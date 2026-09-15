using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace One77.Core.Bundles
{
    /// <summary>
    /// Business rules for Bundle curation: "ONE77 recommends buying these
    /// Shopify products together" — name/tagline/status/order/composition
    /// only. No price, savings, or media. Publish visibility is bundle-level
    /// only in V1; per-item Shopify availability is deferred to the future
    /// Shopify assembly layer (see MATCH_PARITY_REPORT.md-style reasoning —
    /// documented in the Milestone 7 report).
    /// </summary>
    public sealed class BundleService
    {
        private readonly BundleRepository _repository;

        public BundleService(BundleRepository repository)
        {
            _repository = repository;
        }

        public Task<List<Bundle>> GetPublicBundlesAsync() => _repository.GetPublishedAsync();

        public async Task<List<Bundle>> GetAdminBundlesAsync(string status, string nameContains)
        {
            var bundles = await _repository.GetAllAsync();
            if (BundleStatus.IsValid(status))
            {
                bundles = bundles.Where(b => b.Status == status).ToList();
            }
            if (!string.IsNullOrWhiteSpace(nameContains))
            {
                bundles = bundles.Where(b => (b.Name ?? "").ToLowerInvariant().Contains(nameContains.ToLowerInvariant())).ToList();
            }
            return bundles;
        }

        public async Task<Bundle> GetByIdAsync(int id)
        {
            var bundle = await _repository.GetByIdAsync(id);
            if (bundle == null)
            {
                throw new BundleNotFoundException("Bundle not found.");
            }
            return bundle;
        }

        public async Task<Bundle> CreateAsync(string name, string tagline, string status, int sortPriority, List<BundleItemSpec> items)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new BundleValidationException("Please enter a bundle name.");
            }
            var effectiveStatus = string.IsNullOrWhiteSpace(status) ? BundleStatus.Draft : status;
            if (!BundleStatus.IsValid(effectiveStatus))
            {
                throw new BundleValidationException("Invalid status.");
            }
            ValidateItems(items);
            if (effectiveStatus == BundleStatus.Published)
            {
                ValidatePublishable(name, items);
            }

            var id = await _repository.InsertBundleAsync(name.Trim(), tagline ?? "", effectiveStatus, sortPriority);
            await _repository.ReplaceItemsAsync(id, items ?? new List<BundleItemSpec>());

            return await _repository.GetByIdAsync(id);
        }

        /// <summary>
        /// Partial update: a null field leaves the current value unchanged
        /// (same convention used throughout this migration). Passing a
        /// non-null Items list fully replaces the bundle's composition;
        /// pass an empty list to clear it, or null to leave it as-is.
        /// </summary>
        public async Task<Bundle> UpdateAsync(int id, string name, string tagline, string status, int? sortPriority, List<BundleItemSpec> items)
        {
            var current = await _repository.GetByIdAsync(id);
            if (current == null)
            {
                throw new BundleNotFoundException("Bundle not found.");
            }
            if (status != null && !BundleStatus.IsValid(status))
            {
                throw new BundleValidationException("Invalid status.");
            }
            if (items != null)
            {
                ValidateItems(items);
            }

            var effectiveName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : current.Name;
            var effectiveTagline = tagline ?? current.Tagline;
            var effectiveStatus = status ?? current.Status;
            var effectiveSortPriority = sortPriority ?? current.SortPriority;
            var effectiveItems = items ?? current.Items.Select(i => new BundleItemSpec { ShopifyProductId = i.ShopifyProductId, ItemRole = i.ItemRole }).ToList();

            if (effectiveStatus == BundleStatus.Published)
            {
                ValidatePublishable(effectiveName, effectiveItems);
            }

            await _repository.UpdateBundleCoreAsync(id, effectiveName, effectiveTagline, effectiveStatus, effectiveSortPriority);
            if (items != null)
            {
                await _repository.ReplaceItemsAsync(id, items);
            }

            return await _repository.GetByIdAsync(id);
        }

        public async Task<Bundle> PublishAsync(int id)
        {
            var bundle = await GetByIdAsync(id);
            ValidatePublishable(bundle.Name, bundle.Items.Select(i => new BundleItemSpec { ShopifyProductId = i.ShopifyProductId, ItemRole = i.ItemRole }).ToList());
            await _repository.SetStatusAsync(id, BundleStatus.Published);
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Bundle> ArchiveAsync(int id)
        {
            await GetByIdAsync(id);
            await _repository.SetStatusAsync(id, BundleStatus.Archived);
            return await _repository.GetByIdAsync(id);
        }

        /// <summary>Restores to Draft, never straight back to Published — mirrors the reference system's product/bundle unarchive safety rule: an admin must explicitly re-publish.</summary>
        public async Task<Bundle> UnarchiveAsync(int id)
        {
            await GetByIdAsync(id);
            await _repository.SetStatusAsync(id, BundleStatus.Draft);
            return await _repository.GetByIdAsync(id);
        }

        private static void ValidateItems(List<BundleItemSpec> items)
        {
            if (items == null)
            {
                return;
            }
            var seen = new HashSet<string>();
            var primaryCount = 0;
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ShopifyProductId))
                {
                    throw new BundleValidationException("Each bundle item needs a product.");
                }
                if (!seen.Add(item.ShopifyProductId))
                {
                    throw new BundleValidationException("A product cannot be added to the same bundle twice.");
                }
                if (!BundleItemRole.IsValid(item.ItemRole))
                {
                    throw new BundleValidationException("Each item's role must be Primary, Pellet, or Accessory.");
                }
                if (item.ItemRole == BundleItemRole.Primary)
                {
                    primaryCount++;
                }
            }
            if (primaryCount > 1)
            {
                throw new BundleValidationException("A bundle can only have one primary product.");
            }
        }

        /// <summary>Translates the reference bundle_publish_validation_errors rule (must have a main item + at least one other item) into the new Primary/Pellet/Accessory vocabulary — no price validation, since V1 has no bundle price.</summary>
        private static void ValidatePublishable(string name, List<BundleItemSpec> items)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new BundleValidationException("Please enter a bundle name.");
            }
            var itemList = items ?? new List<BundleItemSpec>();
            if (!itemList.Any(i => i.ItemRole == BundleItemRole.Primary))
            {
                throw new BundleValidationException("Please choose a primary product for this bundle.");
            }
            if (itemList.Count < 2)
            {
                throw new BundleValidationException("Add at least one more product alongside the primary product.");
            }
        }
    }
}
