using One77.Core.Admin;
using Xunit;

namespace One77.Core.Tests.Admin
{
    /// <summary>
    /// Tests the portable header-parsing + validation logic a production
    /// host's authentication attribute delegates to. No database, no HTTP
    /// hosting needed — this is exactly the surface that keeps the actual
    /// Web API 2 attribute a thin, mostly-untested-by-necessity adapter.
    /// </summary>
    public class AdminBearerAuthenticatorTests
    {
        private static JwtTokenService Jwt() => new(new JwtOptions
        {
            SigningSecret = "unit-test-signing-secret-not-a-real-secret-0123456789",
            Issuer = "one77-api-tests",
            Audience = "one77-admin-tests"
        });

        private static AdminUser Admin() => new() { AdminUserId = 7, Email = "a@b.example", Name = "A", IsActive = true };

        [Fact]
        public void Authenticate_WithMissingHeader_ReturnsNull()
        {
            Assert.Null(AdminBearerAuthenticator.Authenticate(null!, Jwt()));
            Assert.Null(AdminBearerAuthenticator.Authenticate("", Jwt()));
        }

        [Fact]
        public void Authenticate_WithoutBearerScheme_ReturnsNull()
        {
            var jwt = Jwt();
            var token = jwt.Issue(Admin());

            Assert.Null(AdminBearerAuthenticator.Authenticate(token, jwt)); // no "Bearer " prefix
            Assert.Null(AdminBearerAuthenticator.Authenticate("Basic " + token, jwt));
        }

        [Fact]
        public void Authenticate_WithInvalidToken_ReturnsNull()
        {
            Assert.Null(AdminBearerAuthenticator.Authenticate("Bearer not-a-real-token", Jwt()));
        }

        [Fact]
        public void Authenticate_WithValidBearerToken_ReturnsClaims()
        {
            var jwt = Jwt();
            var admin = Admin();
            var token = jwt.Issue(admin);

            var claims = AdminBearerAuthenticator.Authenticate("Bearer " + token, jwt);

            Assert.NotNull(claims);
            Assert.Equal(admin.AdminUserId, claims!.AdminUserId);
        }
    }
}
