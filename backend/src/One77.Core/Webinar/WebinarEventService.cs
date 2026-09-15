using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace One77.Core.Webinar
{
    public sealed class WebinarEventService
    {
        private readonly WebinarRepository _repository;

        public WebinarEventService(WebinarRepository repository)
        {
            _repository = repository;
        }

        public Task<List<WebinarEvent>> GetPublicUpcomingEventsAsync()
        {
            return _repository.GetUpcomingActiveEventsAsync();
        }

        public Task<List<WebinarEvent>> GetAdminEventsAsync()
        {
            return _repository.GetAllEventsAsync();
        }

        public async Task<WebinarEvent> GetByIdAsync(int id)
        {
            var webinarEvent = await _repository.GetEventByIdAsync(id);
            if (webinarEvent == null)
            {
                throw new WebinarNotFoundException("Webinar event not found.");
            }
            return webinarEvent;
        }

        public async Task<WebinarEvent> CreateAsync(string title, DateTime eventDateTime, string joinLink, string status)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new WebinarValidationException("Title is required.");
            }

            var effectiveStatus = string.IsNullOrWhiteSpace(status) ? WebinarEventStatus.Active : status;
            if (!WebinarEventStatus.IsValid(effectiveStatus))
            {
                throw new WebinarValidationException("Invalid status.");
            }

            var id = await _repository.InsertEventAsync(title, eventDateTime, joinLink, effectiveStatus);
            return await _repository.GetEventByIdAsync(id);
        }

        // Partial-update convention (matches LearnCategoryService/LearnEntryService,
        // Milestone 3): a null/omitted field leaves the current value unchanged.
        // As with Learn's Description field, this means JoinLink cannot be
        // explicitly cleared back to blank via this endpoint in V1 — a real but
        // minor limitation, consistent with the established pattern rather than
        // a one-off "provided" flag for a single field.
        public async Task<WebinarEvent> UpdateAsync(int id, string title, DateTime? eventDateTime, string joinLink, string status)
        {
            var current = await _repository.GetEventByIdAsync(id);
            if (current == null)
            {
                throw new WebinarNotFoundException("Webinar event not found.");
            }

            if (status != null && !WebinarEventStatus.IsValid(status))
            {
                throw new WebinarValidationException("Invalid status.");
            }

            var updated = new WebinarEvent
            {
                EventId = current.EventId,
                Title = !string.IsNullOrWhiteSpace(title) ? title : current.Title,
                EventDateTime = eventDateTime.HasValue ? eventDateTime.Value : current.EventDateTime,
                JoinLink = joinLink != null ? joinLink : current.JoinLink,
                Status = status != null ? status : current.Status
            };

            await _repository.UpdateEventAsync(updated);
            return await _repository.GetEventByIdAsync(id);
        }
    }
}
