using System;
using System.Linq;

namespace One77.Core.Learn
{
    /// <summary>
    /// Learn status vocabulary, per Milestone 2's schema: stored literally as
    /// Draft/Live/Hidden with no Published/Archived translation layer.
    /// </summary>
    public static class LearnStatus
    {
        public const string Draft = "Draft";
        public const string Live = "Live";
        public const string Hidden = "Hidden";

        public static readonly string[] All = { Draft, Live, Hidden };

        public static bool IsValid(string status)
        {
            return status != null && All.Contains(status, StringComparer.Ordinal);
        }
    }
}
