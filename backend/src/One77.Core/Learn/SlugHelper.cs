using System.Text.RegularExpressions;

namespace One77.Core.Learn
{
    /// <summary>
    /// Mirrors the reference system's slugify() exactly: lowercase, collapse
    /// any run of non-alphanumeric characters to a single hyphen, trim
    /// leading/trailing hyphens. Returns "" (not a fallback) when nothing
    /// usable remains — callers decide the fallback, as the reference system's
    /// Learn-specific caller does ("topic").
    /// </summary>
    public static class SlugHelper
    {
        public static string Slugify(string text)
        {
            var s = (text ?? "").ToLowerInvariant().Trim();
            s = Regex.Replace(s, "[^a-z0-9]+", "-");
            s = Regex.Replace(s, "-+", "-").Trim('-');
            return s;
        }
    }
}
