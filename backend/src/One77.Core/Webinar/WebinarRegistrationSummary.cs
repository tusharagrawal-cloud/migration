using System;

namespace One77.Core.Webinar
{
    /// <summary>Admin registration-list row shape: the operational fields only (no EventId — already scoped by the request).</summary>
    public sealed class WebinarRegistrationSummary
    {
        public int RegistrationId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
