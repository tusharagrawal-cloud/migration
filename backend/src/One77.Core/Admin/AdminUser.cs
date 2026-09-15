using System;

namespace One77.Core.Admin
{
    /// <summary>
    /// A ONE77 admin account. V1 is a single/small-admin system — no roles,
    /// no permissions, no claims table. IsActive lets an account be disabled
    /// without deleting it (and without a status-enum column).
    /// </summary>
    public sealed class AdminUser
    {
        public int AdminUserId { get; set; }
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
