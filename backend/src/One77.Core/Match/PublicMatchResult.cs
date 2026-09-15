using System.Collections.Generic;

namespace One77.Core.Match
{
    /// <summary>Mirrors the reference resolve_public_matches() response shape exactly.</summary>
    public sealed class PublicMatchResult
    {
        public MatchProductRef Source { get; set; }
        public List<MatchCard> Pellets { get; set; }
        public List<MatchCard> Accessories { get; set; }
        public List<MatchCard> BestMatchPellets { get; set; }
        public List<MatchCard> RecommendedPellets { get; set; }
        public List<MatchCard> CompatiblePellets { get; set; }
        public List<MatchCard> BestMatchAccessories { get; set; }
        public List<MatchCard> RecommendedAccessories { get; set; }
        public List<MatchCard> CompatibleAccessories { get; set; }
        public string MatchReason { get; set; }
        public string ReasonSource { get; set; }
    }
}
