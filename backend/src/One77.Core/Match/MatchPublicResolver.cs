using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace One77.Core.Match
{
    /// <summary>
    /// Mirrors backend/match_routes.py resolve_public_matches() as closely
    /// as the new architecture allows. Every rule below (sort order, curated-
    /// vs-derived selection, bucketing, use-case filtering, calibre/
    /// powerplant defaulting) is reproduced from reading the actual old
    /// code, not from a summary — see migration/docs/MATCH_PARITY_REPORT.md
    /// for the fixture-by-fixture verification.
    /// </summary>
    public sealed class MatchPublicResolver
    {
        private const string DefaultCalibreValue = "4.5mm";
        private const string DefaultPowerplantValue = "springer";
        private const string DefaultPelletUseCase = "plinking";

        private readonly MatchRepository _repository;

        public MatchPublicResolver(MatchRepository repository)
        {
            _repository = repository;
        }

        public async Task<PublicMatchResult> ResolveAsync(string shopifyProductId, string useCase)
        {
            var source = await _repository.GetProductRefByShopifyIdAsync(shopifyProductId);
            if (source == null || source.Category != MatchCategory.Airgun)
            {
                throw new MatchNotFoundException("Airgun not found");
            }

            var rels = await _repository.GetActiveRelationshipsForSourceAsync(shopifyProductId, null);
            var eligible = rels.Where(r => r.Status != MatchStatus.NotRecommended)
                .OrderBy(r => StatusRank(r))
                .ThenBy(r => UseCaseBoost(r, useCase))
                .ThenBy(r => r.Priority ?? 3)
                .ThenBy(r => r.CreatedAt)
                .ToList();

            var (pBest, pRec, pCompat) = await LoadCuratedTargetsAsync(eligible, MatchCategory.Pellet);
            var (aBest, aRec, aCompat) = await LoadCuratedTargetsAsync(eligible, MatchCategory.Accessory);

            var curatedPellets = pBest.Count > 0 || pRec.Count > 0 || pCompat.Count > 0;
            var curatedAcc = aBest.Count > 0 || aRec.Count > 0 || aCompat.Count > 0;
            var reasonSource = (curatedPellets || curatedAcc) ? "curated" : "derived";

            if (curatedPellets)
            {
                pBest = FilterByUseCase(pBest, useCase);
                pRec = FilterByUseCase(pRec, useCase);
                pCompat = FilterByUseCase(pCompat, useCase);
            }
            else
            {
                pCompat = await DerivePelletFallbackAsync(source);
                pBest = new List<MatchCard>();
                pRec = new List<MatchCard>();
            }

            if (curatedAcc)
            {
                aBest = FilterByUseCase(aBest, useCase);
                aRec = FilterByUseCase(aRec, useCase);
                aCompat = FilterByUseCase(aCompat, useCase);
            }
            else
            {
                aCompat = await DeriveAccessoryFallbackAsync(source);
                aBest = new List<MatchCard>();
                aRec = new List<MatchCard>();
            }

            var allPellets = pBest.Concat(pRec).Concat(pCompat).ToList();
            var allAccessories = aBest.Concat(aRec).Concat(aCompat).ToList();

            var displayCalibre = DefaultCalibre(source.Calibre);
            var matchReason = reasonSource == "curated"
                ? $"Matched by ONE77 · calibre {displayCalibre} · curated."
                : $"Matched by calibre {displayCalibre}, use case {string.Join(", ", source.UseCases ?? new List<string>())}.";

            return new PublicMatchResult
            {
                Source = source,
                Pellets = allPellets,
                Accessories = allAccessories,
                BestMatchPellets = pBest,
                RecommendedPellets = pRec,
                CompatiblePellets = pCompat,
                BestMatchAccessories = aBest,
                RecommendedAccessories = aRec,
                CompatibleAccessories = aCompat,
                MatchReason = matchReason,
                ReasonSource = reasonSource
            };
        }

        private static int StatusRank(MatchRelationship r) => r.Status == MatchStatus.Recommended ? 0 : 1;

        private static int UseCaseBoost(MatchRelationship r, string useCase) =>
            (!string.IsNullOrEmpty(useCase) && r.UseCases != null && r.UseCases.Contains(useCase)) ? 0 : 1;

        private async Task<(List<MatchCard> Best, List<MatchCard> Rec, List<MatchCard> Compat)> LoadCuratedTargetsAsync(
            List<MatchRelationship> sortedEligibleRels, string targetCategory)
        {
            var categoryRels = sortedEligibleRels.Where(r => r.TargetCategory == targetCategory).ToList();
            var best = new List<MatchCard>();
            var rec = new List<MatchCard>();
            var compat = new List<MatchCard>();
            if (categoryRels.Count == 0)
            {
                return (best, rec, compat);
            }

            // Active-only: this is the ONE77-owned substitute for the
            // reference system's Shopify-lifecycle target filter
            // (status=published, active=true) — see MATCH_PARITY_REPORT.md.
            var targets = await _repository.GetProductRefsByShopifyIdsAsync(
                categoryRels.Select(r => r.TargetShopifyProductId), activeOnly: true);
            var byId = targets.ToDictionary(t => t.ShopifyProductId);

            foreach (var r in categoryRels)
            {
                if (!byId.TryGetValue(r.TargetShopifyProductId, out var product))
                {
                    continue;
                }

                var card = new MatchCard
                {
                    Product = product,
                    Status = r.Status,
                    Priority = r.Priority,
                    PriorityLabel = MatchPriorityLabel.For(r.Priority),
                    UseCases = r.UseCases ?? new List<string>(),
                    Reason = r.Reason ?? "",
                    IsCurated = true
                };

                if (r.Status == MatchStatus.Recommended && (r.Priority ?? 3) == 1) best.Add(card);
                else if (r.Status == MatchStatus.Recommended) rec.Add(card);
                else compat.Add(card);
            }

            return (best, rec, compat);
        }

        /// <summary>
        /// Keeps items matching the requested use case, then items with no
        /// use-case restriction at all; drops everything else. Mirrors
        /// _filter_uc exactly — this is a hard filter, not just reordering.
        /// </summary>
        private static List<MatchCard> FilterByUseCase(List<MatchCard> items, string useCase)
        {
            if (string.IsNullOrEmpty(useCase))
            {
                return items;
            }
            var specific = items.Where(i => i.UseCases != null && i.UseCases.Contains(useCase));
            var general = items.Where(i => i.UseCases == null || i.UseCases.Count == 0);
            return specific.Concat(general).ToList();
        }

        /// <summary>
        /// Derived pellet fallback: calibre exact match (both sides defaulted
        /// to 4.5mm when unset) AND the pellet's single use-case tag is in
        /// the airgun's use-case list AND weight within the airgun's
        /// recommended range (0-999 when either bound is unset). No
        /// use-case filter param applied here — the use-case check is
        /// already baked into the fallback rule itself.
        /// </summary>
        private async Task<List<MatchCard>> DerivePelletFallbackAsync(MatchProductRef source)
        {
            decimal lo, hi;
            if (source.RecommendedPelletWeightMin.HasValue && source.RecommendedPelletWeightMax.HasValue)
            {
                lo = source.RecommendedPelletWeightMin.Value;
                hi = source.RecommendedPelletWeightMax.Value;
            }
            else
            {
                lo = 0m;
                hi = 999m;
            }

            var sourceCalibre = DefaultCalibre(source.Calibre);
            var sourceUseCases = source.UseCases ?? new List<string>();

            var pellets = await _repository.GetProductRefsByCategoryAsync(MatchCategory.Pellet, activeOnly: true);
            var derived = new List<(MatchProductRef Product, decimal Weight)>();

            foreach (var p in pellets)
            {
                var pelletCalibre = DefaultCalibre(p.Calibre);
                var pelletUseCase = (p.UseCases != null && p.UseCases.Count > 0) ? p.UseCases[0] : DefaultPelletUseCase;
                var weight = p.WeightGrains ?? 0m;

                if (pelletCalibre == sourceCalibre && sourceUseCases.Contains(pelletUseCase) && weight >= lo && weight <= hi)
                {
                    derived.Add((p, weight));
                }
            }

            return derived
                .OrderBy(d => d.Weight)
                .Select(d => new MatchCard
                {
                    Product = d.Product,
                    Status = null,
                    Priority = null,
                    PriorityLabel = "",
                    UseCases = new List<string>(),
                    Reason = "Derived from calibre and weight compatibility.",
                    IsCurated = false
                })
                .ToList();
        }

        /// <summary>
        /// Derived accessory fallback: the airgun's powerplant type (defaulted
        /// to "springer" when unset) must be in the accessory's list of
        /// compatible powerplants. No calibre/weight/use-case involvement —
        /// matches the reference rule exactly.
        /// </summary>
        private async Task<List<MatchCard>> DeriveAccessoryFallbackAsync(MatchProductRef source)
        {
            var sourcePowerplant = DefaultPowerplant(source.PowerplantType);
            var accessories = await _repository.GetProductRefsByCategoryAsync(MatchCategory.Accessory, activeOnly: true);

            return accessories
                .Where(a => a.CompatiblePowerplants != null && a.CompatiblePowerplants.Contains(sourcePowerplant))
                .Select(a => new MatchCard
                {
                    Product = a,
                    Status = null,
                    Priority = null,
                    PriorityLabel = "",
                    UseCases = new List<string>(),
                    Reason = "Derived from category compatibility.",
                    IsCurated = false
                })
                .ToList();
        }

        private static string DefaultCalibre(string calibre) => string.IsNullOrEmpty(calibre) ? DefaultCalibreValue : calibre;
        private static string DefaultPowerplant(string powerplant) => string.IsNullOrEmpty(powerplant) ? DefaultPowerplantValue : powerplant;
    }
}
