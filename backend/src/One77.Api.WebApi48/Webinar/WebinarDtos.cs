using System;
using One77.Core.Webinar;

namespace One77.Api.WebApi48.Webinar
{
    public sealed class PublicWebinarEvent
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public DateTime EventDateTime { get; set; }
        public string JoinLink { get; set; }

        public static PublicWebinarEvent From(WebinarEvent e) => new PublicWebinarEvent
        {
            EventId = e.EventId,
            Title = e.Title,
            EventDateTime = e.EventDateTime,
            JoinLink = e.JoinLink
        };
    }

    public sealed class CreateWebinarEventRequest
    {
        public string Title { get; set; }
        public DateTime EventDateTime { get; set; }
        public string JoinLink { get; set; }
        public string Status { get; set; }
    }

    public sealed class UpdateWebinarEventRequest
    {
        public string Title { get; set; }
        public DateTime? EventDateTime { get; set; }
        public string JoinLink { get; set; }
        public string Status { get; set; }
    }

    public sealed class RegisterWebinarRequest
    {
        public int? EventId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
