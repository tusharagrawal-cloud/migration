namespace One77.Core.Bundles
{
    /// <summary>Input shape for setting a bundle's items (create/update) — array order is the display order; SortOrder is assigned from position, not supplied by the caller.</summary>
    public sealed class BundleItemSpec
    {
        public string ShopifyProductId { get; set; }
        public string ItemRole { get; set; }
    }
}
