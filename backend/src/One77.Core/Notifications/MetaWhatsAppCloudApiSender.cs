using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace One77.Core.Notifications
{
    // ============================================================================
    // Sends the order-confirmation WhatsApp message via Meta's official WhatsApp
    // Cloud API (Graph API), as a pre-approved template message.
    //
    // Why a template and not a plain text message: Meta only allows free-form
    // text when the *customer* messaged the business first, within the last 24
    // hours. An order confirmation is business-initiated, so it must go through
    // a template that has been submitted and approved in Meta Business Manager
    // (WhatsApp Manager > Message Templates) beforehand. The template referenced
    // here (WhatsAppOptions.TemplateName) must exist and be APPROVED, with its
    // body variables in the same order this class sends them
    // (customer name, order number, total) — e.g.:
    //   "Hi {{1}}, your ONE77 order {{2}} for {{3}} is confirmed!"
    //
    // Failure here (network error, template not approved, number not opted in,
    // etc.) is caught by the caller (OrderNotificationService) and never
    // allowed to block the SMS channel or fail the webhook response to
    // Shopify.
    // ============================================================================
    public sealed class MetaWhatsAppCloudApiSender : IWhatsAppSender
    {
        private readonly HttpClient _httpClient;
        private readonly WhatsAppOptions _options;

        public MetaWhatsAppCloudApiSender(HttpClient httpClient, WhatsAppOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task SendOrderConfirmationAsync(OrderConfirmation order)
        {
            if (string.IsNullOrWhiteSpace(order.CustomerPhoneE164))
            {
                // No phone number collected at checkout — nothing to send to.
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.PhoneNumberId) || string.IsNullOrWhiteSpace(_options.AccessToken))
            {
                throw new InvalidOperationException("WhatsApp:PhoneNumberId / WhatsApp:AccessToken are not configured.");
            }

            var url = $"https://graph.facebook.com/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";

            var totalFormatted = order.Total.ToString("N2", CultureInfo.InvariantCulture);

            var payload = new
            {
                messaging_product = "whatsapp",
                to = ToWhatsAppNumber(order.CustomerPhoneE164),
                type = "template",
                template = new
                {
                    name = _options.TemplateName,
                    language = new { code = _options.TemplateLanguageCode },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new object[]
                            {
                                new { type = "text", text = order.CustomerName },
                                new { type = "text", text = order.OrderNumber },
                                new { type = "text", text = $"{order.CurrencyCode} {totalFormatted}" }
                            }
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new InvalidOperationException($"WhatsApp Cloud API returned {(int)response.StatusCode}: {body}");
            }
        }

        /// <summary>Meta expects the destination in international format with no leading '+'.</summary>
        private static string ToWhatsAppNumber(string phoneE164)
        {
            return phoneE164.StartsWith("+", StringComparison.Ordinal) ? phoneE164.Substring(1) : phoneE164;
        }
    }
}
