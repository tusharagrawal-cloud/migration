using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using One77.Api.WebApi48.Auth;
using One77.Core.Homepage;

namespace One77.Api.WebApi48.Homepage
{
    [RoutePrefix("api")]
    public sealed class HomepageController : ApiController
    {
        // ---------- Public ----------

        [Route("homepage")]
        [HttpGet]
        public async Task<IHttpActionResult> GetPublicConfig() =>
            Ok(ToPublicConfig(await CompositionRoot.HomepageConfigService.GetAsync()));

        // ---------- Admin ----------

        [Route("admin/homepage")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAdminConfig() =>
            Ok(ToPublicConfig(await CompositionRoot.HomepageConfigService.GetAsync()));

        [Route("admin/homepage/hero-image")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> UploadHeroImage()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HomepageValidationException("Please choose an image to upload.");
            }

            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);
            var filePart = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition?.FileName != null);
            if (filePart == null)
            {
                throw new HomepageValidationException("Please choose an image to upload.");
            }

            var bytes = await filePart.ReadAsByteArrayAsync();
            var config = await CompositionRoot.HomepageConfigService.SetHeroImageAsync(bytes);
            return Ok(ToPublicConfig(config));
        }

        [Route("admin/homepage/hero-image")]
        [HttpDelete]
        [JwtAuthorize]
        public async Task<IHttpActionResult> RemoveHeroImage() =>
            Ok(ToPublicConfig(await CompositionRoot.HomepageConfigService.RemoveHeroImageAsync()));

        private static PublicHomepageConfig ToPublicConfig(HomepageConfig config) => new PublicHomepageConfig
        {
            HeroImageUrl = config.HeroImagePath
        };
    }
}
