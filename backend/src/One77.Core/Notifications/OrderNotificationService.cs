using System;
using System.Threading.Tasks;

namespace One77.Core.Notifications
{
    // ============================================================================
    // The single entry point the order webhook calls. Deliberately swallows
    // and logs each channel's own failure rather than letting one bubble up:
    // a WhatsApp template rejection must never stop the SMS from going out,
    // and neither should ever turn into a 500 back to Shopify (Shopify
    // retries failed webhook deliveries repeatedly, which would just resend
    // the same order and duplicate messages).
    // ============================================================================
    public sealed class OrderNotificationService
    {
        private readonly IWhatsAppSender _whatsAppSender;
        private readonly ISmsSender _smsSender;
        private readonly OrderNotificationOptions _options;
        private readonly Action<string, Exception> _logError;

        public OrderNotificationService(
            IWhatsAppSender whatsAppSender,
            ISmsSender smsSender,
            OrderNotificationOptions options,
            Action<string, Exception> logError = null)
        {
            _whatsAppSender = whatsAppSender;
            _smsSender = smsSender;
            _options = options;
            _logError = logError ?? ((_, __) => { });
        }

        public async Task SendOrderConfirmationAsync(OrderConfirmation order)
        {
            if (_options.WhatsAppEnabled)
            {
                try
                {
                    await _whatsAppSender.SendOrderConfirmationAsync(order).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logError($"WhatsApp order confirmation failed for order {order.OrderNumber}.", ex);
                }
            }

            if (_options.SmsEnabled)
            {
                try
                {
                    await _smsSender.SendOrderConfirmationAsync(order).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logError($"SMS order confirmation failed for order {order.OrderNumber}.", ex);
                }
            }
        }
    }
}
