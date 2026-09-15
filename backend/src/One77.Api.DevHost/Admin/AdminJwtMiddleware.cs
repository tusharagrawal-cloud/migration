using System.Security.Claims;
using One77.Core.Admin;

namespace One77.Api.DevHost.Admin;

// ============================================================================
// Protects every /api/admin/* route except /api/admin/login, mirroring
// One77.Api.WebApi48's JwtAuthorizeAttribute behavior exactly. All real
// validation logic lives in One77.Core.Admin.AdminBearerAuthenticator /
// JwtTokenService — this middleware is a thin ASP.NET Core adapter around
// it, just as JwtAuthorizeAttribute is a thin System.Web.Http adapter
// around the same shared Core logic. Deliberately a plain custom
// middleware, not the full ASP.NET Core authentication/authorization
// framework — this host has always favored minimal, understandable
// middleware over a framework for its own sake.
// ============================================================================
public static class AdminJwtMiddleware
{
    public static void UseAdminJwtAuth(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var isAdminRoute = path.StartsWithSegments("/api/admin");
            var isLoginRoute = path.StartsWithSegments("/api/admin/login");

            if (!isAdminRoute || isLoginRoute)
            {
                await next(context);
                return;
            }

            var jwtTokenService = context.RequestServices.GetRequiredService<JwtTokenService>();
            var headerValue = context.Request.Headers.Authorization.ToString();
            var claims = AdminBearerAuthenticator.Authenticate(headerValue, jwtTokenService);

            if (claims == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ErrorResponse { Error = "Authentication required." });
                return;
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, claims.AdminUserId.ToString()),
                new Claim(ClaimTypes.Email, claims.Email)
            }, authenticationType: "Jwt");
            context.User = new ClaimsPrincipal(identity);

            await next(context);
        });
    }
}
