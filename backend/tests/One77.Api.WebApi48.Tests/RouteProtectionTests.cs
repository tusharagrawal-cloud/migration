using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using One77.Api.WebApi48.Auth;
using Xunit;

namespace One77.Api.WebApi48.Tests
{
    /// <summary>
    /// Reflection-only checks over every controller's action methods — no
    /// HTTP hosting, no CompositionRoot/config dependency, so these both
    /// compile AND could execute correctly on any .NET Framework runtime
    /// (Windows) without needing IIS. They exist specifically to catch the
    /// exact mistake Section 15 warns about: an admin route accidentally
    /// left unprotected. Every "admin/" route must carry [JwtAuthorize];
    /// every route that ISN'T "admin/" must not.
    /// </summary>
    public class RouteProtectionTests
    {
        private static IEnumerable<(MethodInfo Method, string Route)> AllRoutedActions()
        {
            var controllerTypes = typeof(JwtAuthorizeAttribute).Assembly
                .GetTypes()
                .Where(t => typeof(ApiController).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var controllerType in controllerTypes)
            {
                var prefix = controllerType.GetCustomAttribute<RoutePrefixAttribute>()?.Prefix ?? "";
                foreach (var method in controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var route = method.GetCustomAttribute<RouteAttribute>();
                    if (route == null) { continue; }

                    var fullRoute = string.IsNullOrEmpty(prefix) ? route.Template : $"{prefix}/{route.Template}";
                    yield return (method, fullRoute);
                }
            }
        }

        // The one deliberate exception: logging in is itself an "admin/" route
        // but must be reachable without a token — you cannot present a JWT
        // before you have one.
        private const string LoginRoute = "api/admin/login";

        [Fact]
        public void EveryAdminRoute_CarriesJwtAuthorize_ExceptLogin()
        {
            var unprotected = AllRoutedActions()
                .Where(a => a.Route.StartsWith("api/admin/", StringComparison.OrdinalIgnoreCase))
                .Where(a => !string.Equals(a.Route, LoginRoute, StringComparison.OrdinalIgnoreCase))
                .Where(a => a.Method.GetCustomAttribute<JwtAuthorizeAttribute>() == null)
                .Select(a => $"{a.Method.DeclaringType?.Name}.{a.Method.Name} ({a.Route})")
                .ToList();

            Assert.True(unprotected.Count == 0, "Unprotected admin route(s): " + string.Join(", ", unprotected));
        }

        [Fact]
        public void NoNonAdminRoute_CarriesJwtAuthorize()
        {
            var wronglyProtected = AllRoutedActions()
                .Where(a => !a.Route.StartsWith("api/admin/", StringComparison.OrdinalIgnoreCase))
                .Where(a => a.Method.GetCustomAttribute<JwtAuthorizeAttribute>() != null)
                .Select(a => $"{a.Method.DeclaringType?.Name}.{a.Method.Name} ({a.Route})")
                .ToList();

            Assert.True(wronglyProtected.Count == 0, "Non-admin route(s) unexpectedly protected: " + string.Join(", ", wronglyProtected));
        }

        [Fact]
        public void LoginRoute_IsNotProtected()
        {
            var login = AllRoutedActions().Single(a => string.Equals(a.Route, LoginRoute, StringComparison.OrdinalIgnoreCase));
            Assert.Null(login.Method.GetCustomAttribute<JwtAuthorizeAttribute>());
        }

        [Fact]
        public void ExpectedPublicRoutes_AreAllPresent()
        {
            var publicRoutes = AllRoutedActions()
                .Where(a => !a.Route.StartsWith("api/admin/", StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Route)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var expected = new[]
            {
                "api/learn/categories",
                "api/learn/categories/{slug}",
                "api/webinar/events",
                "api/webinar/register",
                "api/products/matches",
                "api/products/enrichment",
                "api/bundles"
            };

            foreach (var route in expected)
            {
                Assert.Contains(route, publicRoutes);
            }
        }
    }
}
