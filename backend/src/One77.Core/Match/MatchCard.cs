using System.Collections.Generic;

namespace One77.Core.Match
{
    /// <summary>One matched target (pellet or accessory) with its match metadata — mirrors the old resolver's per-item "match" object.</summary>
    public sealed class MatchCard
    {
        public MatchProductRef Product { get; set; }
        public string Status { get; set; }
        public int? Priority { get; set; }
        public string PriorityLabel { get; set; }
        public List<string> UseCases { get; set; }
        public string Reason { get; set; }
        public bool IsCurated { get; set; }
    }
}
