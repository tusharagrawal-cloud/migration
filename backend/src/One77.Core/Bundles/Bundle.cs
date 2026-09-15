using System;
using System.Collections.Generic;

namespace One77.Core.Bundles
{
    /// <summary>Maps to dbo.Bundles (see Milestone 2, 003_bundles.sql) plus its items.</summary>
    public sealed class Bundle
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Tagline { get; set; }
        public string Status { get; set; }
        public int SortPriority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<BundleItem> Items { get; set; }
    }
}
