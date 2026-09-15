using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace One77.Core.Admin
{
    /// <summary>
    /// Issues and validates the V1 admin JWT. Pure, host-agnostic token
    /// logic — no HTTP, no System.Web — so it is fully unit-testable today
    /// under the net8 dev/test tooling and reusable unchanged by whichever
    /// production host wires it into its request pipeline. Fixed algorithm:
    /// HMAC-SHA256 (HS256), a symmetric algorithm appropriate for a single
    /// API validating its own tokens (no need for asymmetric key
    /// distribution — nothing else consumes these tokens).
    /// </summary>
    public sealed class JwtTokenService
    {
        private readonly JwtOptions _options;

        public JwtTokenService(JwtOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SigningSecret))
            {
                throw new InvalidOperationException("JwtOptions.SigningSecret must be configured.");
            }
            _options = options;
        }

        /// <summary>
        /// Issues a signed token for the given admin. Claims are limited to
        /// what the API needs to identify the caller — subject (admin id),
        /// email, a fixed "admin" role marker (harmless in a single-role
        /// system, but conventional for middleware to key on), issued-at,
        /// and expiry. Never includes the password hash or any other
        /// business data.
        /// </summary>
        public string Issue(AdminUser admin)
        {
            var now = DateTime.UtcNow;
            var expires = now.Add(_options.ExpiresAfter);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, admin.AdminUserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, admin.Email),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(JwtRegisteredClaimNames.Iat, ToUnixSeconds(now).ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Validates signature, issuer, audience, and expiry. Returns null
        /// for anything invalid (bad signature, wrong issuer/audience,
        /// expired, malformed) — callers must not leak which case applied,
        /// per the Admin error-behavior requirement.
        /// </summary>
        public AdminTokenClaims Validate(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningSecret));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
            };

            try
            {
                // Deliberately read claims from the raw JwtSecurityToken, not the
                // returned ClaimsPrincipal: JwtSecurityTokenHandler silently remaps
                // short claim names ("sub", "email") to long XML-namespace claim-type
                // URIs on the principal by default, so principal.FindFirst("sub")
                // returns null even for a token that plainly contains a "sub" claim.
                // The raw token's own Claims collection keeps the original names.
                new JwtSecurityTokenHandler().ValidateToken(token, parameters, out var validatedToken);
                var jwt = (JwtSecurityToken)validatedToken;

                var subject = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                var email = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
                if (subject == null || !int.TryParse(subject, out var adminUserId))
                {
                    return null;
                }

                return new AdminTokenClaims
                {
                    AdminUserId = adminUserId,
                    Email = email ?? "",
                    IssuedAtUtc = jwt.IssuedAt,
                    ExpiresAtUtc = jwt.ValidTo
                };
            }
            catch (SecurityTokenException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static long ToUnixSeconds(DateTime utc) =>
            (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
