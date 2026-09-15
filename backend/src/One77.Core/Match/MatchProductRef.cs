using System.Collections.Generic;

namespace One77.Core.Match
{
    /// <summary>
    /// ONE77-owned product reference data only — no Shopify commerce fields
    /// (name, brand, price, images, etc.) are duplicated here; those are
    /// deferred to the later Shopify Product Detail merge milestone. See
    /// migration/docs/SHOPIFY_V1_CONTRACT.md and MATCH_PARITY_REPORT.md.
    /// </summary>
    public sealed class MatchProductRef
    {
        public string ShopifyProductId { get; set; }
        public string Category { get; set; }
        public bool IsActive { get; set; }
        public string Calibre { get; set; }
        public string PowerplantType { get; set; }
        public decimal? WeightGrains { get; set; }
        public decimal? RecommendedPelletWeightMin { get; set; }
        public decimal? RecommendedPelletWeightMax { get; set; }
        public List<string> UseCases { get; set; }
        public List<string> CompatiblePowerplants { get; set; }
    }
}
