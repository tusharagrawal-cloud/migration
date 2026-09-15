using System;
using System.Collections.Generic;

namespace One77.Core.Enrichment
{
    /// <summary>
    /// One coherent record: dbo.ProductEnrichment plus its child tables
    /// (ProductSpecifications, ProductCompatiblePowerplants, ProductUseCases)
    /// assembled into a single admin-editable shape — "Choose Product → Edit
    /// ONE77 Details → Save", not four separate table endpoints.
    /// </summary>
    public sealed class EnrichmentRecord
    {
        public int Id { get; set; }
        public string ShopifyProductId { get; set; }
        public string Category { get; set; }
        public bool IsActive { get; set; }
        public string Calibre { get; set; }
        public string PowerplantType { get; set; }
        public decimal? WeightGrains { get; set; }
        public decimal? RecommendedPelletWeightMin { get; set; }
        public decimal? RecommendedPelletWeightMax { get; set; }
        public List<string> CompatiblePowerplants { get; set; }
        public List<string> UseCases { get; set; }
        public List<EnrichmentSpecification> Specifications { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
