using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using One77.Api.WebApi48.Auth;

namespace One77.Api.WebApi48
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            // JSON casing: snake_case, matching One77.Api.DevHost and every
            // documented API contract (sort_priority, category_id, ...).
            // Newtonsoft.Json is Web API 2's natural formatter choice — no
            // second serializer is configured.
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
            jsonSettings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };
            jsonSettings.NullValueHandling = NullValueHandling.Include;

            // CORS: allowed origins are entirely configuration-driven (Web.config
            // appSettings/CorsAllowedOrigins) — no hardcoded production domain,
            // no localhost assumption baked into production behavior, and never
            // combined with a wildcard origin (EnableCorsAttribute below always
            // receives an explicit origin list, never "*", when credentials/
            // headers are allowed).
            var allowedOrigins = AppConfig.CorsAllowedOrigins;
            var originsList = allowedOrigins.Length > 0 ? string.Join(",", allowedOrigins) : string.Empty;
            var cors = new EnableCorsAttribute(originsList, headers: "*", methods: "*");
            config.EnableCors(cors);

            config.Filters.Add(new ApiExceptionFilterAttribute());
        }
    }
}
