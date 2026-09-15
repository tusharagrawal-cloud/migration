using System;

namespace One77.Core.Webinar
{
    /// <summary>Result of a successful public registration — minimal, no PII echoed back beyond what the customer already sent.</summary>
    public sealed class WebinarRegistrationResult
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public DateTime EventDateTime { get; set; }
    }
}
