using System;

namespace One77.Core.Webinar
{
    /// <summary>Maps 1:1 to dbo.WebinarEvents (see Milestone 2, 005_webinar.sql).</summary>
    public sealed class WebinarEvent
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public DateTime EventDateTime { get; set; }
        public string JoinLink { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
