using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using One77.Core.Notifications;

namespace One77.Api.DevHost.Orders;

// ============================================================================
// Public-facing (no admin JWT — Shopify's servers call this directly), but
// gated entirely by HMAC verification against Shopify:WebhookSecret. Wired
// up in Shopify Admin as a webhook subscribed to the "Order payment" (or
// "Order creation") topic, pointing at POST {this host}/webhooks/shopify/orders-paid.
//
// Always returns 200 once the payload is verified and parsed, even if a
// notification channel fails inside OrderNotificationService — a non-2xx
// response makes Shopify retry delivery of the *same* order repeatedly,
// which would just resend duplicate WhatsApp/SMS messages rather than fix
// anything. Channel failures are logged, not surfaced to Shopify.
// ============================================================================
public static class OrderWebhookEndpoints
{
    private static readonly JsonSerializerOptions ShopifyJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void MapOrderWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/webhooks/shopify/orders-paid", HandleOrderPaidAsync);
    }

    private static async Task<IResult> HandleOrderPaidAsync(
        HttpRequest request,
        IConfiguration config,
        OrderNotificationService notifications,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("OrderWebhook");

        request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync();
        }
        request.Body.Position = 0;

        var webhookSecret = config["Shopify:WebhookSecret"];
        var hmacHeader = request.Headers["X-Shopify-Hmac-Sha256"].ToString();

        if (!ShopifyWebhookVerifier.IsValid(rawBody, hmacHeader, webhookSecret))
        {
            logger.LogWarning("Rejected Shopify order webhook: HMAC verification failed.");
            return Results.Unauthorized();
        }

        ShopifyOrderWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ShopifyOrderWebhookPayload>(rawBody, ShopifyJsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Rejected Shopify order webhook: payload did not parse.");
            return Results.BadRequest();
        }

        if (payload is null)
        {
            return Results.BadRequest();
        }

        var confirmation = ShopifyOrderMapper.ToOrderConfirmation(payload);

        // Fire-and-forget-safe: OrderNotificationService itself catches and
        // logs each channel's failure, so this call never throws.
        await notifications.SendOrderConfirmationAsync(confirmation);

        return Results.Ok();
    }
}
