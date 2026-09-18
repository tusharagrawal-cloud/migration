using System.Globalization;
using One77.Core.Notifications;

namespace One77.Api.DevHost.Orders;

public static class ShopifyOrderMapper
{
    public static OrderConfirmation ToOrderConfirmation(ShopifyOrderWebhookPayload payload)
    {
        var customerName = BuildCustomerName(payload.Customer?.FirstName, payload.Customer?.LastName);

        // Prefer the customer's own phone; fall back to the shipping address
        // phone — Shopify checkout often only captures one of the two
        // depending on how the customer filled the form.
        var phone = FirstNonEmpty(payload.Customer?.Phone, payload.ShippingAddress?.Phone);

        var items = new List<OrderLineItem>();
        foreach (var li in payload.LineItems)
        {
            items.Add(new OrderLineItem
            {
                Title = li.Title,
                Quantity = li.Quantity,
                Price = ParseDecimalOrZero(li.Price)
            });
        }

        return new OrderConfirmation
        {
            OrderNumber = payload.Name,
            CustomerName = customerName,
            CustomerPhoneE164 = NormalizeToE164(phone),
            Total = ParseDecimalOrZero(payload.TotalPrice),
            CurrencyCode = string.IsNullOrWhiteSpace(payload.Currency) ? "INR" : payload.Currency,
            PaymentMethod = payload.Gateway ?? "unknown",
            Items = items
        };
    }

    private static string BuildCustomerName(string? firstName, string? lastName)
    {
        var full = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(full) ? "Customer" : full;
    }

    private static string? FirstNonEmpty(string? a, string? b)
    {
        if (!string.IsNullOrWhiteSpace(a)) return a;
        if (!string.IsNullOrWhiteSpace(b)) return b;
        return null;
    }

    private static decimal ParseDecimalOrZero(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;
    }

    /// <summary>
    /// Shopify stores phone numbers in whatever format the customer typed
    /// them in — normalizes the common India-only cases actually seen at
    /// this storefront's checkout. A phone number in a format not
    /// recognized here is passed through as-is; both senders already treat
    /// a clearly-invalid number as a provider-side failure for that order
    /// only, not a reason to fail the whole webhook.
    /// </summary>
    private static string? NormalizeToE164(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (phone.TrimStart().StartsWith("+"))
        {
            return "+" + digits;
        }

        if (digits.Length == 10)
        {
            return "+91" + digits;
        }

        if (digits.Length == 12 && digits.StartsWith("91"))
        {
            return "+" + digits;
        }

        return "+" + digits;
    }
}
