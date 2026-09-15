namespace One77.Core.Match
{
    /// <summary>Admin airgun-list row: ONE77-owned reference fields plus computed completeness. Name/brand/image are deferred to the later Shopify merge.</summary>
    public sealed class MatchAirgunSummaryItem
    {
        public string ShopifyProductId { get; set; }
        public string Calibre { get; set; }
        public string PowerplantType { get; set; }
        public MatchCompleteness Counts { get; set; }
    }
}
