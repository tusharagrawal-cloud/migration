using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using One77.Api.WebApi48.Auth;
using One77.Core.Bundles;

namespace One77.Api.WebApi48.Bundles
{
    [RoutePrefix("api")]
    public sealed class BundlesController : ApiController
    {
        // ---------- Public ----------

        [Route("bundles")]
        [HttpGet]
        public async Task<IHttpActionResult> GetPublicBundles()
        {
            var bundles = await CompositionRoot.BundleService.GetPublicBundlesAsync();
            return Ok(bundles.Select(ToPublicBundle));
        }

        // ---------- Admin ----------

        [Route("admin/bundles")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAdminBundles(
            [FromUri(Name = "status")] string status,
            [FromUri(Name = "q")] string q)
        {
            var items = await CompositionRoot.BundleService.GetAdminBundlesAsync(status, q);
            return Ok(new { items, count = items.Count });
        }

        [Route("admin/bundles/{id:int}")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAdminBundle(int id) =>
            Ok(await CompositionRoot.BundleService.GetByIdAsync(id));

        [Route("admin/bundles")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> Create(CreateBundleRequest req) =>
            Ok(await CompositionRoot.BundleService.CreateAsync(req.Name, req.Tagline, req.Status, req.SortPriority, ToCoreItems(req.Items)));

        [Route("admin/bundles/{id:int}")]
        [HttpPut]
        [JwtAuthorize]
        public async Task<IHttpActionResult> Update(int id, UpdateBundleRequest req) =>
            Ok(await CompositionRoot.BundleService.UpdateAsync(id, req.Name, req.Tagline, req.Status, req.SortPriority, ToCoreItems(req.Items)));

        [Route("admin/bundles/{id:int}/publish")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> Publish(int id) =>
            Ok(await CompositionRoot.BundleService.PublishAsync(id));

        [Route("admin/bundles/{id:int}/archive")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> Archive(int id) =>
            Ok(await CompositionRoot.BundleService.ArchiveAsync(id));

        [Route("admin/bundles/{id:int}/unarchive")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> Unarchive(int id) =>
            Ok(await CompositionRoot.BundleService.UnarchiveAsync(id));

        private static System.Collections.Generic.List<BundleItemSpec> ToCoreItems(System.Collections.Generic.List<BundleItemInput> items) =>
            items?.Select(i => new BundleItemSpec { ShopifyProductId = i.ShopifyProductId, ItemRole = i.ItemRole }).ToList();

        private static PublicBundle ToPublicBundle(Bundle b) => new PublicBundle
        {
            Id = b.Id,
            Name = b.Name,
            Tagline = b.Tagline ?? "",
            SortPriority = b.SortPriority,
            Items = b.Items.Select(i => new PublicBundleItem { ShopifyProductId = i.ShopifyProductId, ItemRole = i.ItemRole, SortOrder = i.SortOrder }).ToList()
        };
    }
}
