namespace One77.Core.Match
{
    /// <summary>
    /// Priority 1/2/3 are only meaningful when Status = recommended. These
    /// customer-facing labels are derived at read time, exactly as in the
    /// reference system's PRIORITY_LABELS dict — never stored.
    /// </summary>
    public static class MatchPriorityLabel
    {
        public const string BestMatch = "Best Match";
        public const string Recommended = "Recommended";
        public const string Alternative = "Alternative";

        public static string For(int? priority)
        {
            switch (priority)
            {
                case 1: return BestMatch;
                case 2: return Recommended;
                case 3: return Alternative;
                default: return "";
            }
        }
    }
}
