using System;

namespace One77.Core.Webinar
{
    /// <summary>Maps 1:1 to dbo.WebinarRegistrations (see Milestone 2, 005_webinar.sql).</summary>
    public sealed class WebinarRegistration
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
