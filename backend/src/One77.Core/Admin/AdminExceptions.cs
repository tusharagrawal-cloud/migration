using System;

namespace One77.Core.Admin
{
    /// <summary>
    /// Login failed — bad email, bad password, or a disabled account. Always
    /// carries the same generic message regardless of which of those it was;
    /// the API layer must map this to 401 without ever revealing which
    /// reason applied (see Milestone 10's Admin error-behavior requirement:
    /// never confirm whether a given email exists).
    /// </summary>
    public sealed class AdminAuthenticationException : Exception
    {
        public AdminAuthenticationException(string message) : base(message) { }
    }
}
