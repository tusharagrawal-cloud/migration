using System;

namespace One77.Core.Learn
{
    /// <summary>Admin entry-list row shape: entry fields plus its parent category's name.</summary>
    public sealed class LearnEntryWithCategoryName
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int SortPriority { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CategoryName { get; set; }
    }
}
