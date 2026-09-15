using System.Collections.Generic;

namespace One77.Api.WebApi48.Match
{
    // Field naming note: request/response fields use "shopify_product_id"
    // (via SnakeCaseNamingStrategy on the response side, and explicit query
    // parameter names on the request side) — same contract as
    // One77.Api.DevHost. See MATCH_PARITY_REPORT.md.

    public sealed class UpsertMatchRelationshipRequest
    {
        public string SourceShopifyProductId { get; set; }
        public string TargetShopifyProductId { get; set; }
        public string Status { get; set; }
        public int? Priority { get; set; }
        public List<string> UseCases { get; set; }
        public string Reason { get; set; }
        public string AdminNotes { get; set; }
        public bool CalibreOverride { get; set; }
    }

    public sealed class PatchMatchRelationshipRequest
    {
        public string Status { get; set; }
        public int? Priority { get; set; }
        public List<string> UseCases { get; set; }
        public string Reason { get; set; }
        public string AdminNotes { get; set; }
        public bool? CalibreOverride { get; set; }
        public bool? Active { get; set; }
    }

    public sealed class BulkMatchRequest
    {
        public string SourceShopifyProductId { get; set; }
        public List<string> TargetShopifyProductIds { get; set; } = new List<string>();
        public string Status { get; set; } = "compatible";
    }
}
