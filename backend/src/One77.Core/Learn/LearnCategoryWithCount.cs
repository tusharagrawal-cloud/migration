using System;

namespace One77.Core.Learn
{
    /// <summary>Admin category-list row shape: category fields plus its entry count.</summary>
    public sealed class LearnCategoryWithCount
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public int SortPriority { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int EntryCount { get; set; }
    }
}
