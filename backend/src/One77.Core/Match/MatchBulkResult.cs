namespace One77.Core.Match
{
    /// <summary>Mirrors the reference bulk_mark() response shape.</summary>
    public sealed class MatchBulkResult
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int SkippedCalibre { get; set; }
    }
}
