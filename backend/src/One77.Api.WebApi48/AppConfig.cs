using System;
using System.Configuration;
using System.Web.Hosting;

namespace One77.Api.WebApi48
{
    /// <summary>
    /// Reads Web.config (connectionStrings/appSettings) once. Every value
    /// here is environment-specific and must be supplied by whoever
    /// deploys this host — see migration/docs/PRODUCTION_HOST_V1.md for
    /// exactly what Tushar must provide and where.
    /// </summary>
    public static class AppConfig
    {
        public static string SqlConnectionString =>
            ConfigurationManager.ConnectionStrings["Default"]?.ConnectionString
                ?? throw new InvalidOperationException("connectionStrings/Default is not configured in Web.config.");

        public static string JwtSigningSecret =>
            Require("JwtSigningSecret");

        public static string JwtIssuer =>
            ConfigurationManager.AppSettings["JwtIssuer"] ?? "one77-api";

        public static string JwtAudience =>
            ConfigurationManager.AppSettings["JwtAudience"] ?? "one77-admin";

        public static TimeSpan JwtExpiry =>
            TimeSpan.FromMinutes(ParseIntOrDefault("JwtExpiryMinutes", 720)); // 720 min = 12 hours, matching the old app's session length

        /// <summary>Comma-separated list of allowed CORS origins — no wildcard, environment-specific, never hardcoded here.</summary>
        public static string[] CorsAllowedOrigins
        {
            get
            {
                var raw = ConfigurationManager.AppSettings["CorsAllowedOrigins"] ?? "";
                var origins = Array.ConvertAll(raw.Split(','), o => o.Trim());
                return Array.FindAll(origins, o => o.Length > 0);
            }
        }

        /// <summary>
        /// First-admin seed credentials. Left empty in every committed
        /// config — Tushar supplies these only for the initial deployment
        /// (see PRODUCTION_HOST_V1.md); AdminAuthService.EnsureSeedAdminAsync
        /// only ever acts on them when the AdminUsers table is empty.
        /// </summary>
        public static string InitialAdminEmail => ConfigurationManager.AppSettings["InitialAdminEmail"] ?? "";
        public static string InitialAdminPassword => ConfigurationManager.AppSettings["InitialAdminPassword"] ?? "";
        public static string InitialAdminName => ConfigurationManager.AppSettings["InitialAdminName"] ?? "Admin";

        /// <summary>
        /// Physical folder the homepage hero photograph is written to.
        /// Configurable (never hardcoded to Tushar's real server path) — left
        /// blank, defaults to "&lt;site physical root&gt;/media", a sibling of
        /// bin/ and the Angular build output that neither a WebApi48 nor an
        /// Angular redeploy ever touches. See PRODUCTION_CONFIGURATION.md.
        /// </summary>
        public static string HomepageMediaPhysicalRoot
        {
            get
            {
                var configured = ConfigurationManager.AppSettings["HomepageMediaPhysicalRoot"];
                return string.IsNullOrWhiteSpace(configured)
                    ? HostingEnvironment.MapPath("~/media")
                    : configured;
            }
        }

        /// <summary>URL path prefix the media folder is served under — IIS serves it as a normal static file, same mechanism as the Angular build's own assets.</summary>
        public static string HomepageMediaUrlPrefix => "/media";

        /// <summary>Maximum accepted hero-image upload size, in bytes. Default 8 MB.</summary>
        public static long HomepageMediaMaxBytes => ParseLongOrDefault("HomepageMediaMaxBytes", 8 * 1024 * 1024);

        private static string Require(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"appSettings/{key} is not configured in Web.config.");
            }
            return value;
        }

        private static int ParseIntOrDefault(string key, int defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            return int.TryParse(raw, out var parsed) ? parsed : defaultValue;
        }

        private static long ParseLongOrDefault(string key, long defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            return long.TryParse(raw, out var parsed) ? parsed : defaultValue;
        }
    }
}
