using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using One77.Api.WebApi48.Auth;
using One77.Core.Admin;
using Xunit;

namespace One77.Api.WebApi48.Tests
{
    /// <summary>
    /// Exercises JwtAuthorizeAttribute directly (no real HTTP hosting) using
    /// Web API 2's own testable object model — HttpActionContext and its
    /// dependencies are ordinary POCOs designed to be constructed outside
    /// IIS. This still requires the real .NET Framework CLR to run, which
    /// this Linux sandbox lacks (no Mono) — compile-verified only here.
    /// </summary>
    public class AuthAttributeTests
    {
        private static HttpActionContext CreateContext(HttpRequestMessage request)
        {
            var config = new HttpConfiguration();
            request.SetConfiguration(config);
            var routeData = new HttpRouteData(new HttpRoute());
            var controllerContext = new HttpControllerContext(config, routeData, request)
            {
                Request = request
            };
            var actionContext = new HttpActionContext
            {
                ControllerContext = controllerContext
            };
            return actionContext;
        }

        [Fact]
        public void MissingAuthorizationHeader_Returns401()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/admin/learn/categories");
            var context = CreateContext(request);

            new JwtAuthorizeAttribute().OnAuthorization(context);

            Assert.NotNull(context.Response);
            Assert.Equal(HttpStatusCode.Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public void InvalidBearerToken_Returns401()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/admin/learn/categories");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");
            var context = CreateContext(request);

            new JwtAuthorizeAttribute().OnAuthorization(context);

            Assert.NotNull(context.Response);
            Assert.Equal(HttpStatusCode.Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public void ValidBearerToken_SetsPrincipal_AndDoesNotShortCircuit()
        {
            var admin = new AdminUser { AdminUserId = 99, Email = "admin@one77.example", Name = "A", IsActive = true };
            var token = CompositionRoot.JwtTokenService.Issue(admin);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/admin/learn/categories");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var context = CreateContext(request);

            new JwtAuthorizeAttribute().OnAuthorization(context);

            Assert.Null(context.Response);
            Assert.NotNull(context.ControllerContext.RequestContext.Principal);
            Assert.Equal(admin.AdminUserId, context.ControllerContext.RequestContext.Principal.CurrentAdminUserId());
        }
    }
}
