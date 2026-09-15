using System.Collections.Generic;

namespace One77.Core.Learn
{
    /// <summary>Result shape for the public category-detail lookup (category + its live entries).</summary>
    public sealed class LearnCategoryDetail
    {
        public LearnCategory Category { get; set; }
        public List<LearnEntry> Entries { get; set; }
    }
}
