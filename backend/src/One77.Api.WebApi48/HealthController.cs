using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using One77.Core.Diagnostics;

namespace One77.Api.WebApi48
{
    /// <summary>
    /// Deployment smoke-check endpoints only — same contract as
    /// One77.Api.DevHost's /health and /health/db, so a deployer can use
    /// either host's response shape interchangeably. Not a monitoring
    /// system: no auth (deliberately, so IIS/network health probes work
    /// without a token), no historical data, no dependency beyond the SQL
    /// Server connectivity check already used everywhere else.
    /// </summary>
    [RoutePrefix("health")]
    public sealed class HealthController : ApiController
    {
        private static readonly DatabaseConnectivityChecker Checker = new DatabaseConnectivityChecker(CompositionRoot.ConnectionFactory);

        [Route("")]
        [HttpGet]
        public IHttpActionResult Get()
        {
            return Ok(new { status = "ok", host = "webapi48" });
        }

        [Route("db")]
        [HttpGet]
        public async Task<IHttpActionResult> GetDb()
        {
            try
            {
                var connected = await Checker.CanConnectAsync();
                if (connected)
                {
                    return Ok(new { status = "ok", database = "reachable" });
                }

                return Content(HttpStatusCode.ServiceUnavailable, new ErrorResponse { Error = "Database unreachable." });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.ServiceUnavailable, new ErrorResponse { Error = ex.Message });
            }
        }
    }
}
