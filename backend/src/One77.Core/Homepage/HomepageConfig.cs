using System;

namespace One77.Core.Homepage
{
    /// <summary>Maps to dbo.HomepageConfig (see Milestone 14, 007_homepage.sql) — a singleton row, Id always 1.</summary>
    public sealed class HomepageConfig
    {
        public int Id { get; set; }

        /// <summary>URL-relative path (e.g. "/media/hero-&lt;guid&gt;.jpg"), never a physical file system path. Null = no hero photograph; the storefront falls back to its default dark hero.</summary>
        public string HeroImagePath { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
