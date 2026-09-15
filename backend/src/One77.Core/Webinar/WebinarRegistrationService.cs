using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace One77.Core.Webinar
{
    public sealed class WebinarRegistrationService
    {
        private readonly WebinarRepository _repository;

        public WebinarRegistrationService(WebinarRepository repository)
        {
            _repository = repository;
        }

        // "Has this event already passed?" is evaluated by comparing the
        // event's stored (UTC) EventDateTime against the app server's
        // DateTime.UtcNow. Both sides represent the same UTC instant even
        // though Dapper reads the column back with DateTimeKind.Unspecified,
        // so the comparison is correct; this is a deliberately simple check,
        // not a general timezone-handling layer.
        public async Task<WebinarRegistrationResult> RegisterAsync(int? eventId, string name, string email, string phone)
        {
            if (!eventId.HasValue)
            {
                throw new WebinarValidationException("event_id is required.");
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new WebinarValidationException("Name is required.");
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new WebinarValidationException("Email is required.");
            }
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new WebinarValidationException("Phone is required.");
            }

            var webinarEvent = await _repository.GetEventByIdAsync(eventId.Value);
            if (webinarEvent == null)
            {
                throw new WebinarValidationException("The selected webinar event could not be found.");
            }
            if (webinarEvent.Status != WebinarEventStatus.Active)
            {
                throw new WebinarValidationException("This webinar event is no longer accepting registrations.");
            }
            if (webinarEvent.EventDateTime <= DateTime.UtcNow)
            {
                throw new WebinarValidationException("This webinar event has already taken place.");
            }

            var registrationId = await _repository.InsertRegistrationAsync(
                webinarEvent.EventId, name.Trim(), email.Trim(), phone.Trim());

            return new WebinarRegistrationResult
            {
                RegistrationId = registrationId,
                EventId = webinarEvent.EventId,
                EventTitle = webinarEvent.Title,
                EventDateTime = webinarEvent.EventDateTime
            };
        }

        public async Task<List<WebinarRegistrationSummary>> GetRegistrationsForEventAsync(int eventId)
        {
            var webinarEvent = await _repository.GetEventByIdAsync(eventId);
            if (webinarEvent == null)
            {
                throw new WebinarNotFoundException("Webinar event not found.");
            }

            var registrations = await _repository.GetRegistrationsForEventAsync(eventId);
            return registrations.Select(r => new WebinarRegistrationSummary
            {
                RegistrationId = r.RegistrationId,
                Name = r.Name,
                Email = r.Email,
                Phone = r.Phone,
                RegisteredAt = r.RegisteredAt
            }).ToList();
        }
    }
}
