namespace One77.Api.DevHost.Match;

// Field naming note: request/response fields use "shopify_product_id"
// (not the old system's bare "product_id") to stay consistent with this
// migration's own SQL column names (SourceShopifyProductId etc.) and the
// Milestone 4 Shopify contract's terminology — a deliberate naming
// clarification, not a behavioral change. See MATCH_PARITY_REPORT.md.

public sealed class UpsertMatchRelationshipRequest
{
    public string SourceShopifyProductId { get; set; } = "";
    public string TargetShopifyProductId { get; set; } = "";
    public string Status { get; set; } = "";
    public int? Priority { get; set; }
    public List<string>? UseCases { get; set; }
    public string? Reason { get; set; }
    public string? AdminNotes { get; set; }
    public bool CalibreOverride { get; set; }
}

public sealed class PatchMatchRelationshipRequest
{
    public string? Status { get; set; }
    public int? Priority { get; set; }
    public List<string>? UseCases { get; set; }
    public string? Reason { get; set; }
    public string? AdminNotes { get; set; }
    public bool? CalibreOverride { get; set; }
    public bool? Active { get; set; }
}

public sealed class BulkMatchRequest
{
    public string SourceShopifyProductId { get; set; } = "";
    public List<string> TargetShopifyProductIds { get; set; } = new();
    public string Status { get; set; } = "compatible";
}
