namespace One77.Core.Admin
{
    /// <summary>
    /// BCrypt.Net-Next-backed password hashing — the chosen V1 algorithm
    /// (see migration/docs/PRODUCTION_HOST_V1.md). BCrypt generates and
    /// embeds its own random salt per hash automatically; callers never
    /// handle salts directly.
    /// </summary>
    public sealed class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
