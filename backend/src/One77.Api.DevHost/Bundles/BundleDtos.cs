namespace One77.Api.DevHost.Bundles;

public sealed class BundleItemInput
{
    public string ShopifyProductId { get; set; } = "";
    public string ItemRole { get; set; } = "";
}

public sealed class CreateBundleRequest
{
    public string Name { get; set; } = "";
    public string? Tagline { get; set; }
    public string? Status { get; set; }
    public int SortPriority { get; set; }
    public List<BundleItemInput>? Items { get; set; }
}

public sealed class UpdateBundleRequest
{
    public string? Name { get; set; }
    public string? Tagline { get; set; }
    public string? Status { get; set; }
    public int? SortPriority { get; set; }
    public List<BundleItemInput>? Items { get; set; }
}

// Public shape — omits Status (implied Published) and admin timestamps.
public sealed class PublicBundleItem
{
    public string ShopifyProductId { get; set; } = "";
    public string ItemRole { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class PublicBundle
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
    public int SortPriority { get; set; }
    public List<PublicBundleItem> Items { get; set; } = new();
}
