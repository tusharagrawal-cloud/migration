using System;
using System.Linq;

namespace One77.Core.Webinar
{
    /// <summary>Webinar event status vocabulary, per Milestone 2's schema (005_webinar.sql).</summary>
    public static class WebinarEventStatus
    {
        public const string Active = "Active";
        public const string Cancelled = "Cancelled";
        public const string Completed = "Completed";

        public static readonly string[] All = { Active, Cancelled, Completed };

        public static bool IsValid(string status)
        {
            return status != null && All.Contains(status, StringComparer.Ordinal);
        }
    }
}
