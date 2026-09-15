using System;
using System.Collections.Generic;

namespace One77.Core.Match
{
    /// <summary>Maps to dbo.MatchRelationships (see Milestone 2, 002_match.sql) plus its child use-case tags.</summary>
    public sealed class MatchRelationship
    {
        public int Id { get; set; }
        public string SourceShopifyProductId { get; set; }
        public string TargetShopifyProductId { get; set; }
        public string TargetCategory { get; set; }
        public string Status { get; set; }
        public int? Priority { get; set; }
        public string Reason { get; set; }
        public string AdminNotes { get; set; }
        public bool CalibreOverride { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> UseCases { get; set; }
    }
}
