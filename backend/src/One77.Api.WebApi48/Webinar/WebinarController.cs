using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using One77.Api.WebApi48.Auth;

namespace One77.Api.WebApi48.Webinar
{
    [RoutePrefix("api")]
    public sealed class WebinarController : ApiController
    {
        // ---------- Public ----------

        [Route("webinar/events")]
        [HttpGet]
        public async Task<IHttpActionResult> GetPublicEvents()
        {
            var items = await CompositionRoot.WebinarEventService.GetPublicUpcomingEventsAsync();
            return Ok(items.Select(PublicWebinarEvent.From));
        }

        [Route("webinar/register")]
        [HttpPost]
        public async Task<IHttpActionResult> Register(RegisterWebinarRequest req) =>
            Ok(await CompositionRoot.WebinarRegistrationService.RegisterAsync(req.EventId, req.Name, req.Email, req.Phone));

        // ---------- Admin ----------

        [Route("admin/webinar/events")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAdminEvents() =>
            Ok(await CompositionRoot.WebinarEventService.GetAdminEventsAsync());

        [Route("admin/webinar/events/{id:int}")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAdminEvent(int id) =>
            Ok(await CompositionRoot.WebinarEventService.GetByIdAsync(id));

        [Route("admin/webinar/events")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> CreateEvent(CreateWebinarEventRequest req) =>
            Ok(await CompositionRoot.WebinarEventService.CreateAsync(req.Title, req.EventDateTime, req.JoinLink, req.Status));

        [Route("admin/webinar/events/{id:int}")]
        [HttpPut]
        [JwtAuthorize]
        public async Task<IHttpActionResult> UpdateEvent(int id, UpdateWebinarEventRequest req) =>
            Ok(await CompositionRoot.WebinarEventService.UpdateAsync(id, req.Title, req.EventDateTime, req.JoinLink, req.Status));

        [Route("admin/webinar/events/{id:int}/registrations")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetRegistrations(int id) =>
            Ok(await CompositionRoot.WebinarRegistrationService.GetRegistrationsForEventAsync(id));
    }
}
