namespace One77.Api.DevHost.Webinar;

// EventDateTime convention: UTC throughout — accepted and returned as ISO-8601
// UTC instants (System.Text.Json's default DateTime handling), matching
// CreatedAt/UpdatedAt/RegisteredAt across the whole V1 schema. The later
// Angular UI and WhatsApp integration should treat this value as UTC and
// convert for display/scheduling themselves; no timezone-conversion
// infrastructure is built here.

public sealed class PublicWebinarEvent
{
    public int EventId { get; set; }
    public string Title { get; set; } = "";
    public DateTime EventDateTime { get; set; }
    public string? JoinLink { get; set; }
}

public sealed class CreateWebinarEventRequest
{
    public string Title { get; set; } = "";
    public DateTime EventDateTime { get; set; }
    public string? JoinLink { get; set; }
    public string? Status { get; set; }
}

public sealed class UpdateWebinarEventRequest
{
    public string? Title { get; set; }
    public DateTime? EventDateTime { get; set; }
    public string? JoinLink { get; set; }
    public string? Status { get; set; }
}

public sealed class RegisterWebinarRequest
{
    public int? EventId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

