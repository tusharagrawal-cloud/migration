namespace One77.Core.Admin
{
    /// <summary>
    /// Parses an "Authorization" header value and validates the bearer
    /// token it carries. Deliberately framework-agnostic (a plain string in,
    /// a claims object or null out) so the actual HTTP-pipeline wiring in a
    /// production host's authentication attribute stays a thin adapter
    /// around this, fully testable without any HTTP hosting.
    /// </summary>
    public static class AdminBearerAuthenticator
    {
        private const string BearerPrefix = "Bearer ";

        public static AdminTokenClaims Authenticate(string authorizationHeaderValue, JwtTokenService jwtTokenService)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeaderValue))
            {
                return null;
            }

            if (!authorizationHeaderValue.StartsWith(BearerPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var token = authorizationHeaderValue.Substring(BearerPrefix.Length).Trim();
            if (token.Length == 0)
            {
                return null;
            }

            return jwtTokenService.Validate(token);
        }
    }
}
