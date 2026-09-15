using System;
using System.Linq;

namespace One77.Core.Bundles
{
    /// <summary>Bundle status vocabulary, matching Milestone 2's schema (003_bundles.sql) exactly.</summary>
    public static class BundleStatus
    {
        public const string Draft = "Draft";
        public const string Published = "Published";
        public const string Archived = "Archived";

        public static readonly string[] All = { Draft, Published, Archived };

        public static bool IsValid(string status) => status != null && All.Contains(status, StringComparer.Ordinal);
    }
}
