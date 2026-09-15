using System;
using System.Linq;

namespace One77.Core.Match
{
    /// <summary>
    /// Match relationship status vocabulary — a literal parity requirement,
    /// preserved exactly (lowercase, underscored) from the reference system
    /// (backend/match_routes.py STATUSES), not a new schema design choice.
    /// </summary>
    public static class MatchStatus
    {
        public const string Compatible = "compatible";
        public const string Recommended = "recommended";
        public const string NotRecommended = "not_recommended";

        public static readonly string[] All = { Compatible, Recommended, NotRecommended };

        public static bool IsValid(string status)
        {
            return status != null && All.Contains(status, StringComparer.Ordinal);
        }
    }
}
