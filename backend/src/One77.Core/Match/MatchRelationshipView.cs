using System;
using System.Collections.Generic;

namespace One77.Core.Match
{
    /// <summary>Admin-facing relationship shape: the stored relationship plus its target's product reference.</summary>
    public sealed class MatchRelationshipView
    {
        public int Id { get; set; }
        public string SourceShopifyProductId { get; set; }
        public string TargetShopifyProductId { get; set; }
        public string TargetCategory { get; set; }
        public string Status { get; set; }
        public int? Priority { get; set; }
        public string PriorityLabel { get; set; }
        public string Reason { get; set; }
        public string AdminNotes { get; set; }
        public bool CalibreOverride { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> UseCases { get; set; }
        public MatchProductRef Target { get; set; }
    }
}
