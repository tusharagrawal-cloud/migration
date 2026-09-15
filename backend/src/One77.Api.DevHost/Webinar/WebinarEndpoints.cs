using Microsoft.AspNetCore.Mvc;
using One77.Core.Webinar;

namespace One77.Api.DevHost.Webinar;

// ============================================================================
// DEV-ONLY: none of the /api/admin/webinar/* endpoints below carry any
// authentication or authorization. Production Admin auth is a decision
// reserved for the Phase 3 Section 11 checkpoint (a later milestone) and is
// intentionally NOT implemented here — same boundary as Learn (Milestone 3).
// ============================================================================
public static class WebinarEndpoints
{
    public static void MapWebinarEndpoints(this WebApplication app)
    {
        MapPublicEndpoints(app);
        MapAdminEndpoints(app);
    }

    private static void MapPublicEndpoints(WebApplication app)
    {
        app.MapGet("/api/webinar/events", async (WebinarEventService events) =>
        {
            var items = await events.GetPublicUpcomingEventsAsync();
            return Results.Ok(items.Select(ToPublicEvent));
        });

        app.MapPost("/api/webinar/register", async (RegisterWebinarRequest req, WebinarRegistrationService registrations) =>
            Results.Ok(await registrations.RegisterAsync(req.EventId, req.Name, req.Email, req.Phone)));
    }

    private static void MapAdminEndpoints(WebApplication app)
    {
        app.MapGet("/api/admin/webinar/events", async (WebinarEventService events) =>
            Results.Ok(await events.GetAdminEventsAsync()));

        app.MapGet("/api/admin/webinar/events/{id:int}", async (int id, WebinarEventService events) =>
            Results.Ok(await events.GetByIdAsync(id)));

        app.MapPost("/api/admin/webinar/events", async (CreateWebinarEventRequest req, WebinarEventService events) =>
            Results.Ok(await events.CreateAsync(req.Title, req.EventDateTime, req.JoinLink, req.Status)));

        app.MapPut("/api/admin/webinar/events/{id:int}", async (int id, UpdateWebinarEventRequest req, WebinarEventService events) =>
            Results.Ok(await events.UpdateAsync(id, req.Title, req.EventDateTime, req.JoinLink, req.Status)));

        app.MapGet("/api/admin/webinar/events/{id:int}/registrations", async (int id, WebinarRegistrationService registrations) =>
            Results.Ok(await registrations.GetRegistrationsForEventAsync(id)));
    }

    private static PublicWebinarEvent ToPublicEvent(WebinarEvent e) => new()
    {
        EventId = e.EventId,
        Title = e.Title,
        EventDateTime = e.EventDateTime,
        JoinLink = e.JoinLink
    };
}
