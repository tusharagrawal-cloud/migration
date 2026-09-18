using System.Security.Cryptography;
using System.Text;

namespace One77.Api.DevHost.Orders;

// ============================================================================
// Shopify signs every webhook request with HMAC-SHA256 over the RAW request
// body, base64-encoded, using the webhook's signing secret (Shopify Admin >
// Settings > Notifications > Webhooks, or the secret returned when the
// webhook is created via the Admin API). This must run against the exact
// bytes Shopify sent — never against a re-serialized/re-parsed version of
// the body, since re-serialization can change whitespace/property order and
// silently break verification.
// ============================================================================
public static class ShopifyWebhookVerifier
{
    public static bool IsValid(string rawRequestBody, string? hmacHeaderValue, string? webhookSecret)
    {
        if (string.IsNullOrEmpty(hmacHeaderValue) || string.IsNullOrEmpty(webhookSecret))
        {
            return false;
        }

        var keyBytes = Encoding.UTF8.GetBytes(webhookSecret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawRequestBody);

        byte[] computedHash;
        using (var hmac = new HMACSHA256(keyBytes))
        {
            computedHash = hmac.ComputeHash(bodyBytes);
        }

        var computedBase64 = Convert.ToBase64String(computedHash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedBase64),
            Encoding.UTF8.GetBytes(hmacHeaderValue));
    }
}
