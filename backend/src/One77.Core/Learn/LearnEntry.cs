using System;

namespace One77.Core.Learn
{
    /// <summary>
    /// Maps 1:1 to dbo.LearnEntries (see Milestone 2, 004_learn.sql). The SQL
    /// column is LearnCategoryId; this property is named CategoryId to match
    /// the API's field naming, with the rename done in the repository's SQL
    /// (column alias), not here.
    /// </summary>
    public sealed class LearnEntry
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int SortPriority { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
