using System;

namespace One77.Core.Admin
{
    /// <summary>The minimal identity a validated admin JWT carries — never a password or hash.</summary>
    public sealed class AdminTokenClaims
    {
        public int AdminUserId { get; set; }
        public string Email { get; set; } = "";
        public DateTime IssuedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}
