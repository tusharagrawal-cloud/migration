using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using One77.Core.Match;

namespace One77.Core.Enrichment
{
    /// <summary>
    /// Business rules for Product Enrichment: one coherent record per
    /// Shopify Product ID, category validated against the same
    /// airgun/pellet/accessory vocabulary Match already owns (reused, not
    /// duplicated — see One77.Core.Match.MatchCategory).
    /// </summary>
    public sealed class EnrichmentService
    {
        private readonly EnrichmentRepository _repository;

        public EnrichmentService(EnrichmentRepository repository)
        {
            _repository = repository;
        }

        public Task<List<EnrichmentRecord>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public async Task<EnrichmentRecord> GetByShopifyProductIdAsync(string shopifyProductId)
        {
            var record = await _repository.GetByShopifyIdAsync(shopifyProductId);
            if (record == null)
            {
                throw new EnrichmentNotFoundException("No ONE77 details found for this product.");
            }
            return record;
        }

        /// <summary>Public/internal read for the future Product Detail assembly layer — excludes records the admin has toggled inactive.</summary>
        public async Task<EnrichmentRecord> GetPublicByShopifyProductIdAsync(string shopifyProductId)
        {
            var record = await _repository.GetByShopifyIdAsync(shopifyProductId);
            if (record == null || !record.IsActive)
            {
                throw new EnrichmentNotFoundException("No ONE77 details found for this product.");
            }
            return record;
        }

        public async Task<EnrichmentRecord> CreateAsync(
            string shopifyProductId, string category, bool isActive,
            string calibre, string powerplantType, decimal? weightGrains,
            decimal? recommendedMin, decimal? recommendedMax,
            List<string> compatiblePowerplants, List<string> useCases, List<EnrichmentSpecification> specifications)
        {
            if (string.IsNullOrWhiteSpace(shopifyProductId))
            {
                throw new EnrichmentValidationException("A Shopify product is required.");
            }
            if (!IsValidCategory(category))
            {
                throw new EnrichmentValidationException("Category must be airgun, pellet, or accessory.");
            }
            if (await _repository.ShopifyIdExistsAsync(shopifyProductId))
            {
                throw new EnrichmentValidationException("This product already has ONE77 details — edit the existing record instead.");
            }

            var record = new EnrichmentRecord
            {
                ShopifyProductId = shopifyProductId,
                Category = category,
                IsActive = isActive,
                Calibre = NormalizeOrNull(calibre),
                PowerplantType = NormalizeOrNull(powerplantType),
                WeightGrains = weightGrains,
                RecommendedPelletWeightMin = recommendedMin,
                RecommendedPelletWeightMax = recommendedMax
            };

            var id = await _repository.InsertAsync(record);
            await _repository.ReplaceSpecificationsAsync(id, NormalizeSpecifications(specifications));
            await _repository.ReplaceCompatiblePowerplantsAsync(id, compatiblePowerplants ?? new List<string>());
            await _repository.ReplaceUseCasesAsync(id, useCases ?? new List<string>());

            return await _repository.GetByShopifyIdAsync(shopifyProductId);
        }

        /// <summary>
        /// Partial update: a null field leaves the current value unchanged
        /// (same convention as Learn/Webinar/Match — a field cannot be
        /// explicitly cleared back to null through this endpoint, a known,
        /// accepted limitation consistent with the rest of this migration).
        /// Passing a non-null list for Specifications/CompatiblePowerplants/
        /// UseCases fully replaces that list; pass an empty list to clear
        /// it, or null to leave it as-is.
        /// </summary>
        public async Task<EnrichmentRecord> UpdateAsync(
            string shopifyProductId, string category, bool? isActive,
            string calibre, string powerplantType, decimal? weightGrains,
            decimal? recommendedMin, decimal? recommendedMax,
            List<string> compatiblePowerplants, List<string> useCases, List<EnrichmentSpecification> specifications)
        {
            var current = await _repository.GetByShopifyIdAsync(shopifyProductId);
            if (current == null)
            {
                throw new EnrichmentNotFoundException("No ONE77 details found for this product.");
            }
            if (category != null && !IsValidCategory(category))
            {
                throw new EnrichmentValidationException("Category must be airgun, pellet, or accessory.");
            }

            current.Category = category ?? current.Category;
            current.IsActive = isActive ?? current.IsActive;
            current.Calibre = calibre != null ? NormalizeOrNull(calibre) : current.Calibre;
            current.PowerplantType = powerplantType != null ? NormalizeOrNull(powerplantType) : current.PowerplantType;
            current.WeightGrains = weightGrains ?? current.WeightGrains;
            current.RecommendedPelletWeightMin = recommendedMin ?? current.RecommendedPelletWeightMin;
            current.RecommendedPelletWeightMax = recommendedMax ?? current.RecommendedPelletWeightMax;

            await _repository.UpdateCoreFieldsAsync(current);

            if (specifications != null)
            {
                await _repository.ReplaceSpecificationsAsync(current.Id, NormalizeSpecifications(specifications));
            }
            if (compatiblePowerplants != null)
            {
                await _repository.ReplaceCompatiblePowerplantsAsync(current.Id, compatiblePowerplants);
            }
            if (useCases != null)
            {
                await _repository.ReplaceUseCasesAsync(current.Id, useCases);
            }

            return await _repository.GetByShopifyIdAsync(shopifyProductId);
        }

        private static bool IsValidCategory(string category) =>
            category == MatchCategory.Airgun || category == MatchCategory.Pellet || category == MatchCategory.Accessory;

        private static string NormalizeOrNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private static List<EnrichmentSpecification> NormalizeSpecifications(List<EnrichmentSpecification> specifications)
        {
            if (specifications == null)
            {
                return new List<EnrichmentSpecification>();
            }
            return specifications
                .Where(s => !string.IsNullOrWhiteSpace(s.SpecKey) && !string.IsNullOrWhiteSpace(s.SpecValue))
                .Select(s => new EnrichmentSpecification { SpecKey = s.SpecKey.Trim(), SpecValue = s.SpecValue.Trim(), SortOrder = s.SortOrder })
                .ToList();
        }
    }
}
