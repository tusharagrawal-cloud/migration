using System;
using System.Linq;

namespace One77.Core.Bundles
{
    /// <summary>Bundle item role vocabulary, matching Milestone 2's schema (003_bundles.sql CK_BundleItems_ItemRole) exactly.</summary>
    public static class BundleItemRole
    {
        public const string Primary = "Primary";
        public const string Pellet = "Pellet";
        public const string Accessory = "Accessory";

        public static readonly string[] All = { Primary, Pellet, Accessory };

        public static bool IsValid(string role) => role != null && All.Contains(role, StringComparer.Ordinal);
    }
}
