namespace One77.Core.Match
{
    /// <summary>A candidate target product plus its current relationship to the source airgun, if any.</summary>
    public sealed class MatchCandidateItem
    {
        public MatchProductRef Product { get; set; }
        public MatchRelationshipView CurrentRelationship { get; set; }
    }
}
