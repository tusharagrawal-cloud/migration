using System.Security.Claims;
using One77.Core.Admin;

namespace One77.Api.DevHost.Admin;

// ============================================================================
// Mirrors One77.Api.WebApi48/Auth/AdminAuthController.cs's contract exactly
// (same routes, same request/response shape, same generic 401 behavior) so
// the Angular Admin can be developed and live-verified against this host —
// see Milestone 11's baseline note: the production WebApi48 host cannot be
// runtime-tested in this Linux sandbox, so DevHost needed a real (not
// stubbed) admin auth surface for the first time to make Angular Admin
// end-to-end verification possible at all. Business logic is 100% shared
// with the production host via One77.Core.Admin — only this thin route
// mapping is duplicated, exactly like every other domain in this host.
// ============================================================================
public static class AdminAuthEndpoints
{
    public static void MapAdminAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/admin/login", async (LoginRequest req, AdminAuthService authService, JwtTokenService jwtTokenService) =>
        {
            var admin = await authService.LoginAsync(req.Email, req.Password);
            var token = jwtTokenService.Issue(admin);
            return Results.Ok(new LoginResponse
            {
                AccessToken = token,
                AdminUserId = admin.AdminUserId,
                Email = admin.Email,
                Name = admin.Name
            });
        });

        app.MapGet("/api/admin/me", async (HttpContext context, AdminAuthService authService) =>
        {
            var adminUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (adminUserId == null || !int.TryParse(adminUserId, out var id))
            {
                return Results.Unauthorized();
            }

            var admin = await authService.GetByIdAsync(id);
            return Results.Ok(new AdminMeResponse
            {
                AdminUserId = admin.AdminUserId,
                Email = admin.Email,
                Name = admin.Name
            });
        });
    }
}
