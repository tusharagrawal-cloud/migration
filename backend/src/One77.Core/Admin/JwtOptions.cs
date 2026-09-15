using System;

namespace One77.Core.Admin
{
    /// <summary>
    /// Configuration the production host supplies to <see cref="JwtTokenService"/>.
    /// SigningSecret must never be committed — the host reads it from its own
    /// environment-specific configuration (Web.config on the production
    /// host), never a hardcoded value.
    /// </summary>
    public sealed class JwtOptions
    {
        public string SigningSecret { get; set; } = "";
        public string Issuer { get; set; } = "one77-api";
        public string Audience { get; set; } = "one77-admin";

        /// <summary>
        /// V1 session duration. 720 minutes (12 hours) matches the old
        /// application's session length; kept as the default unless a
        /// concrete security reason argues otherwise (none identified for
        /// V1 — see migration/docs/PRODUCTION_HOST_V1.md).
        /// </summary>
        public TimeSpan ExpiresAfter { get; set; } = TimeSpan.FromHours(12);
    }
}
