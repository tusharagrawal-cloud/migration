using System;
using System.Web.Http;

namespace One77.Api.WebApi48
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            SeedFirstAdminIfConfigured();
        }

        /// <summary>
        /// Runs on every application start, but is a no-op once any admin
        /// exists — see AdminAuthService.EnsureSeedAdminAsync. Only attempts
        /// seeding when both InitialAdminEmail and InitialAdminPassword are
        /// present in Web.config, so a deployment that already has an admin
        /// (and has since removed those settings) is unaffected.
        /// </summary>
        private static void SeedFirstAdminIfConfigured()
        {
            var email = AppConfig.InitialAdminEmail;
            var password = AppConfig.InitialAdminPassword;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            try
            {
                CompositionRoot.AdminAuthService
                    .EnsureSeedAdminAsync(email, password, AppConfig.InitialAdminName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                // Startup must not crash the whole application over a seed
                // failure (e.g. the database not being reachable yet) — log
                // and continue; the admin can be seeded on a later restart.
                System.Diagnostics.Trace.TraceError("First-admin seed failed: " + ex);
            }
        }
    }
}
