namespace One77.Core.Admin
{
    /// <summary>Abstraction over the chosen password hashing algorithm (BCrypt for V1 — see BCryptPasswordHasher).</summary>
    public interface IPasswordHasher
    {
        /// <summary>Returns a salted hash of <paramref name="password"/>. Never returns the raw password.</summary>
        string Hash(string password);

        /// <summary>True if <paramref name="password"/> matches the given hash.</summary>
        bool Verify(string password, string hash);
    }
}
