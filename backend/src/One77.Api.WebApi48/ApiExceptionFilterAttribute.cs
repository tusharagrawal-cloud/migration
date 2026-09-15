using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using One77.Core.Admin;
using One77.Core.Bundles;
using One77.Core.Enrichment;
using One77.Core.Homepage;
using One77.Core.Learn;
using One77.Core.Match;
using One77.Core.Webinar;

namespace One77.Api.WebApi48
{
    /// <summary>
    /// Single, shared exception mapping for the whole host — the same
    /// 404/400/401/500 mapping as One77.Api.DevHost's exception-handler
    /// middleware, so behavior does not drift between the two hosts.
    /// </summary>
    public sealed class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var error = context.Exception;
            HttpStatusCode status;

            if (error is LearnNotFoundException or WebinarNotFoundException or MatchNotFoundException
                or EnrichmentNotFoundException or BundleNotFoundException)
            {
                status = HttpStatusCode.NotFound;
            }
            else if (error is LearnValidationException or WebinarValidationException or MatchValidationException
                or EnrichmentValidationException or BundleValidationException or HomepageValidationException)
            {
                status = HttpStatusCode.BadRequest;
            }
            else if (error is AdminAuthenticationException)
            {
                status = HttpStatusCode.Unauthorized;
            }
            else
            {
                status = HttpStatusCode.InternalServerError;
            }

            var message = status == HttpStatusCode.InternalServerError
                ? "An unexpected error occurred."
                : error.Message;

            context.Response = context.Request.CreateResponse(status, new ErrorResponse { Error = message });
        }
    }
}
