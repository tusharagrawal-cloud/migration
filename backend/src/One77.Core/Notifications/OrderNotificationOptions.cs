namespace One77.Core.Notifications
{
    // ========================================================================
    // Bound from the "Notifications" section of appsettings.json. All values
    // are plain strings (no secrets library yet, matching how Jwt:SigningSecret
    // and ConnectionStrings:Default are handled elsewhere in this project) —
    // real values belong only in appsettings.Development.json (gitignored) or
    // production environment variables, never in the committed template.
    // ========================================================================
    public sealed class WhatsAppOptions
    {
        /// <summary>Meta Graph API phone number ID (from WhatsApp Manager, not the phone number itself).</summary>
        public string PhoneNumberId { get; set; }

        /// <summary>Permanent access token for the WhatsApp Business app (System User token — not the 24h test token).</summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Name of the pre-approved message template (Meta Business Manager
        /// &gt; WhatsApp Manager &gt; Message Templates). Business-initiated
        /// messages like an order confirmation MUST use an approved template —
        /// Meta rejects free-form text outside a 24h customer-initiated
        /// window.
        /// </summary>
        public string TemplateName { get; set; } = "order_confirmation";

        public string TemplateLanguageCode { get; set; } = "en";

        /// <summary>Graph API version, e.g. "v21.0". Meta deprecates old versions periodically.</summary>
        public string ApiVersion { get; set; } = "v21.0";
    }

    public sealed class SmsOptions
    {
        /// <summary>loadcrm.com account API key (the "key" query parameter).</summary>
        public string AuthKey { get; set; }

        public string UserName { get; set; } = "trial";

        /// <summary>Approved DLT sender ID, e.g. "LOADIT".</summary>
        public string SenderId { get; set; }

        /// <summary>DLT-registered Entity ID (Principal Entity ID from the DLT portal).</summary>
        public string EntityId { get; set; }

        /// <summary>DLT-registered Template ID — must match MessageTemplate's wording exactly, or the carrier silently drops the SMS.</summary>
        public string TemplateId { get; set; }

        /// <summary>
        /// The DLT-approved message text, with {0}=customer name, {1}=order
        /// number, {2}=total as placeholders. Must match the wording
        /// registered against TemplateId on the DLT portal exactly — only
        /// the {0}/{1}/{2} values may vary between messages.
        /// </summary>
        public string MessageTemplate { get; set; } =
            "Hi {0}, your ONE77 order {1} for Rs.{2} is confirmed. Load Infotech";

        public bool Unicode { get; set; } = false;

        public string ApiBaseUrl { get; set; } = "https://loadcrm.com";
    }

    public sealed class OrderNotificationOptions
    {
        public bool WhatsAppEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; } = true;

        public WhatsAppOptions WhatsApp { get; set; } = new WhatsAppOptions();
        public SmsOptions Sms { get; set; } = new SmsOptions();
    }
}
