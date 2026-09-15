namespace One77.Core.Bundles
{
    /// <summary>Maps to dbo.BundleItems (see Milestone 2, 003_bundles.sql).</summary>
    public sealed class BundleItem
    {
        public int Id { get; set; }
        public string ShopifyProductId { get; set; }
        public string ItemRole { get; set; }
        public int SortOrder { get; set; }
    }
}
