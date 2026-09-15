using System.Threading.Tasks;
using System.Web.Http;

namespace One77.Api.WebApi48.Auth
{
    [RoutePrefix("api/admin")]
    public sealed class AdminAuthController : ApiController
    {
        [Route("login")]
        [HttpPost]
        public async Task<IHttpActionResult> Login(LoginRequest request)
        {
            // AdminAuthService.LoginAsync throws AdminAuthenticationException
            // (mapped to 401 by ApiExceptionFilterAttribute) for a missing
            // email, wrong password, or disabled account alike — this
            // controller never sees, and so cannot leak, which case applied.
            var admin = await CompositionRoot.AdminAuthService.LoginAsync(request?.Email, request?.Password);
            var token = CompositionRoot.JwtTokenService.Issue(admin);

            return Ok(new LoginResponse
            {
                AccessToken = token,
                AdminUserId = admin.AdminUserId,
                Email = admin.Email,
                Name = admin.Name
            });
        }

        [Route("me")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> Me()
        {
            var adminUserId = RequestContext.Principal.CurrentAdminUserId();
            var admin = await CompositionRoot.AdminAuthService.GetByIdAsync(adminUserId.GetValueOrDefault());

            return Ok(new AdminMeResponse
            {
                AdminUserId = admin.AdminUserId,
                Email = admin.Email,
                Name = admin.Name
            });
        }
    }
}
