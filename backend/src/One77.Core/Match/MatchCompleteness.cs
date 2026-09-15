namespace One77.Core.Match
{
    /// <summary>
    /// Computed, never stored — mirrors the reference _completeness() exactly.
    /// Pellets require >=1 compatible-or-better AND >=1 recommended;
    /// accessories require only >=1 compatible-or-better.
    /// </summary>
    public sealed class MatchCompleteness
    {
        public int CompatPellets { get; set; }
        public int RecPellets { get; set; }
        public int CompatAcc { get; set; }
        public int RecAcc { get; set; }
        public string State { get; set; }
    }
}
