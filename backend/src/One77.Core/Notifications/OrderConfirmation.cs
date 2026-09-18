using System;
using System.Collections.Generic;

namespace One77.Core.Notifications
{
    // ========================================================================
    // The channel-agnostic shape every notification sender (WhatsApp, SMS, and
    // any future channel) works from. Kept deliberately separate from
    // Shopify's own webhook JSON shape (see
    // One77.Api.DevHost/Orders/ShopifyOrderMapper) so a sender never needs to
    // know anything about Shopify's payload — only about an order. If a
    // second commerce source ever exists, only the mapper changes.
    // ========================================================================
    public sealed class OrderConfirmation
    {
        /// <summary>Customer-facing order number, e.g. "#AR4LG0W00".</summary>
        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        /// <summary>
        /// E.164 format (e.g. "+919876543210"). Null when Shopify didn't
        /// collect a phone number at checkout — both senders must treat that
        /// as "skip", not as an error.
        /// </summary>
        public string CustomerPhoneE164 { get; set; }

        public decimal Total { get; set; }

        /// <summary>ISO 4217 currency code, e.g. "INR".</summary>
        public string CurrencyCode { get; set; }

        public string PaymentMethod { get; set; }

        public IReadOnlyList<OrderLineItem> Items { get; set; } = Array.Empty<OrderLineItem>();
    }

    public sealed class OrderLineItem
    {
        public string Title { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
