using System;

namespace One77.Core.Learn
{
    /// <summary>Maps 1:1 to dbo.LearnCategories (see Milestone 2, 004_learn.sql).</summary>
    public sealed class LearnCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public int SortPriority { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
