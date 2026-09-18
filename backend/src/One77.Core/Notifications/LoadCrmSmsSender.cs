using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace One77.Core.Notifications
{
    // ============================================================================
    // Sends the order-confirmation SMS via loadcrm.com's OwnApi (GET request,
    // query-string parameters), e.g.:
    //
    //   GET https://loadcrm.com/SmsApi/api/OwnApi/SendSms
    //       ?key=...&UserName=trial&SenderID=LOADIT&MessageText=...
    //       &EntityId=...&TemplateId=...&Unicode=false&MobileNo=9876543210
    //
    // MessageText must be the DLT-approved template text with only the
    // variable portions filled in (SmsOptions.MessageTemplate) — TRAI
    // requires this for any SMS to an Indian number; a MessageText that
    // doesn't match what's registered against TemplateId on the DLT portal
    // is silently dropped by the carrier, not just rejected by loadcrm.
    //
    // MobileNo is a bare 10-digit Indian number (no country code, no '+'),
    // matching the example loadcrm.com supplied — different from the E.164
    // format WhatsApp's Cloud API expects.
    // ============================================================================
    public sealed class LoadCrmSmsSender : ISmsSender
    {
        private readonly HttpClient _httpClient;
        private readonly SmsOptions _options;

        public LoadCrmSmsSender(HttpClient httpClient, SmsOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task SendOrderConfirmationAsync(OrderConfirmation order)
        {
            if (string.IsNullOrWhiteSpace(order.CustomerPhoneE164))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.AuthKey) || string.IsNullOrWhiteSpace(_options.TemplateId))
            {
                throw new InvalidOperationException("Sms:AuthKey / Sms:TemplateId are not configured.");
            }

            var totalFormatted = order.Total.ToString("N2", CultureInfo.InvariantCulture);
            var messageText = string.Format(_options.MessageTemplate, order.CustomerName, order.OrderNumber, totalFormatted);

            var queryParams = new (string Key, string Value)[]
            {
                ("key", _options.AuthKey),
                ("UserName", _options.UserName),
                ("SenderID", _options.SenderId),
                ("MessageText", messageText),
                ("EntityId", _options.EntityId),
                ("TemplateId", _options.TemplateId),
                ("Unicode", _options.Unicode ? "true" : "false"),
                ("MobileNo", ToTenDigitIndianNumber(order.CustomerPhoneE164))
            };

            var queryString = string.Join("&", Array.ConvertAll(queryParams,
                p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value ?? string.Empty)}"));

            var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/SmsApi/api/OwnApi/SendSms?{queryString}";

            using var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new InvalidOperationException($"loadcrm SMS API returned {(int)response.StatusCode}: {body}");
            }
        }

        /// <summary>loadcrm expects a bare 10-digit number — strips a leading '+91' (or '91') if present.</summary>
        private static string ToTenDigitIndianNumber(string phoneE164)
        {
            var digits = phoneE164.TrimStart('+');
            if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
            {
                digits = digits.Substring(2);
            }
            return digits;
        }
    }
}
