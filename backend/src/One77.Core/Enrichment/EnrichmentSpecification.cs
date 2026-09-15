namespace One77.Core.Enrichment
{
    /// <summary>One flexible SpecKey/SpecValue row (dbo.ProductSpecifications).</summary>
    public sealed class EnrichmentSpecification
    {
        public string SpecKey { get; set; }
        public string SpecValue { get; set; }
        public int SortOrder { get; set; }
    }
}
