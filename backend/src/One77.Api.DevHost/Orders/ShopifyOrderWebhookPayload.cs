using System.Text.Json.Serialization;

namespace One77.Api.DevHost.Orders;

// ============================================================================
// Deliberately only the fields this feature needs, not a full mirror of
// Shopify's order object (which has 80+ fields). Property names use
// Shopify's own snake_case exactly as it sends them — this type is
// deserialized with its own explicit JsonSerializerOptions (see
// OrderWebhookEndpoints.ShopifyJsonOptions), independent of the host's
// global response-naming policy, since this is inbound data from Shopify,
// not an outbound response.
// ============================================================================
public sealed class ShopifyOrderWebhookPayload
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = ""; // e.g. "#AR4LG0W00"

    [JsonPropertyName("total_price")]
    public string TotalPrice { get; set; } = "0";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "INR";

    [JsonPropertyName("gateway")]
    public string? Gateway { get; set; }

    [JsonPropertyName("customer")]
    public ShopifyCustomer? Customer { get; set; }

    [JsonPropertyName("shipping_address")]
    public ShopifyAddress? ShippingAddress { get; set; }

    [JsonPropertyName("line_items")]
    public List<ShopifyLineItem> LineItems { get; set; } = new();
}

public sealed class ShopifyCustomer
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

public sealed class ShopifyAddress
{
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

public sealed class ShopifyLineItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0";
}
