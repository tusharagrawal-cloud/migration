using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using One77.Core.Admin;

namespace One77.Api.WebApi48.Auth
{
    /// <summary>
    /// Protects an action/controller with the V1 admin JWT. All real
    /// validation logic (header parsing, signature/issuer/audience/expiry
    /// checks) lives in One77.Core's AdminBearerAuthenticator/JwtTokenService,
    /// fully unit-tested there — this attribute is deliberately just the
    /// thin System.Web.Http adapter around it: read the Authorization
    /// header, delegate, and translate the result into either a populated
    /// request principal or a 401. Never reveals *why* a token was
    /// rejected (missing/invalid/expired all produce the same 401).
    /// </summary>
    public sealed class JwtAuthorizeAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var headerValue = actionContext.Request.Headers.Authorization?.ToString();
            var claims = AdminBearerAuthenticator.Authenticate(headerValue, CompositionRoot.JwtTokenService);

            if (claims == null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Unauthorized, new ErrorResponse { Error = "Authentication required." });
                return;
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, claims.AdminUserId.ToString()),
                new Claim(ClaimTypes.Email, claims.Email)
            }, authenticationType: "Jwt");

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);
        }
    }

    /// <summary>Small extension so controllers can read "which admin made this request" without touching claim-type strings directly.</summary>
    public static class HttpActionContextExtensions
    {
        public static int? CurrentAdminUserId(this IPrincipal principal)
        {
            if (principal is not ClaimsPrincipal claimsPrincipal)
            {
                return null;
            }

            var value = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
