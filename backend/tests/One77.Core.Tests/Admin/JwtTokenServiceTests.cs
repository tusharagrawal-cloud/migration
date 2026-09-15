using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using One77.Core.Admin;
using Xunit;

namespace One77.Core.Tests.Admin
{
    /// <summary>
    /// Pure logic tests — no database needed. These always run (unlike the
    /// integration tests elsewhere in this suite, which skip without
    /// ONE77_TEST_CONNECTION_STRING).
    /// </summary>
    public class JwtTokenServiceTests
    {
        private static JwtOptions Options(TimeSpan? expiresAfter = null) => new()
        {
            SigningSecret = "unit-test-signing-secret-not-a-real-secret-0123456789",
            Issuer = "one77-api-tests",
            Audience = "one77-admin-tests",
            ExpiresAfter = expiresAfter ?? TimeSpan.FromHours(12)
        };

        private static AdminUser Admin() => new()
        {
            AdminUserId = 42,
            Email = "admin@one77.example",
            Name = "Test Admin",
            IsActive = true
        };

        [Fact]
        public void Issue_ProducesAWellFormedJwt()
        {
            var service = new JwtTokenService(Options());
            var token = service.Issue(Admin());

            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.Equal(3, token.Split('.').Length); // header.payload.signature
        }

        [Fact]
        public void Issue_UsesHmacSha256()
        {
            var service = new JwtTokenService(Options());
            var token = service.Issue(Admin());

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal("HS256", jwt.Header.Alg);
        }

        [Fact]
        public void Issue_ContainsNoPasswordOrHashClaim()
        {
            var service = new JwtTokenService(Options());
            var token = service.Issue(Admin());

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            foreach (var claim in jwt.Claims)
            {
                Assert.DoesNotContain("password", claim.Type, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("hash", claim.Type, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Validate_WithValidToken_ReturnsExpectedClaims()
        {
            var service = new JwtTokenService(Options());
            var admin = Admin();
            var token = service.Issue(admin);

            var claims = service.Validate(token);

            Assert.NotNull(claims);
            Assert.Equal(admin.AdminUserId, claims!.AdminUserId);
            Assert.Equal(admin.Email, claims.Email);
        }

        [Fact]
        public void Validate_ExpiryClaim_IsBoundedByConfiguredDuration()
        {
            var service = new JwtTokenService(Options(TimeSpan.FromHours(12)));
            var beforeIssue = DateTime.UtcNow;
            var token = service.Issue(Admin());
            var afterIssue = DateTime.UtcNow;

            var claims = service.Validate(token)!;

            Assert.InRange(claims.ExpiresAtUtc, beforeIssue.AddHours(12).AddSeconds(-5), afterIssue.AddHours(12).AddSeconds(5));
        }

        [Fact]
        public void Validate_WithTamperedSignature_ReturnsNull()
        {
            var service = new JwtTokenService(Options());
            var token = service.Issue(Admin());

            var tampered = token.Substring(0, token.Length - 4) + "abcd";

            Assert.Null(service.Validate(tampered));
        }

        [Fact]
        public void Validate_WithWrongSigningSecret_ReturnsNull()
        {
            var issuer = new JwtTokenService(Options());
            var token = issuer.Issue(Admin());

            var validatorWithDifferentSecret = new JwtTokenService(
                new JwtOptions { SigningSecret = "a-completely-different-secret-value-0987654321", Issuer = "one77-api-tests", Audience = "one77-admin-tests" });

            Assert.Null(validatorWithDifferentSecret.Validate(token));
        }

        [Fact]
        public void Validate_WithWrongAudience_ReturnsNull()
        {
            var issuer = new JwtTokenService(Options());
            var token = issuer.Issue(Admin());

            var validatorWithDifferentAudience = new JwtTokenService(
                new JwtOptions { SigningSecret = "unit-test-signing-secret-not-a-real-secret-0123456789", Issuer = "one77-api-tests", Audience = "someone-else" });

            Assert.Null(validatorWithDifferentAudience.Validate(token));
        }

        [Fact]
        public void Validate_WithExpiredToken_ReturnsNull()
        {
            var options = Options();
            var service = new JwtTokenService(options);

            // Built independently of JwtTokenService.Issue (which always uses
            // DateTime.UtcNow and would reject a not-before >= expires) so this
            // test can simulate a token whose validity window has genuinely
            // passed — comfortably beyond the 30-second clock-skew tolerance.
            var notBefore = DateTime.UtcNow.AddHours(-2);
            var expires = DateTime.UtcNow.AddMinutes(-5);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiredJwt = new JwtSecurityToken(
                issuer: options.Issuer,
                audience: options.Audience,
                claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "42") },
                notBefore: notBefore,
                expires: expires,
                signingCredentials: credentials);
            var expiredToken = new JwtSecurityTokenHandler().WriteToken(expiredJwt);

            Assert.Null(service.Validate(expiredToken));
        }

        [Fact]
        public void Validate_WithNullOrEmptyToken_ReturnsNull()
        {
            var service = new JwtTokenService(Options());

            Assert.Null(service.Validate(""));
            Assert.Null(service.Validate(null!));
        }
    }
}
