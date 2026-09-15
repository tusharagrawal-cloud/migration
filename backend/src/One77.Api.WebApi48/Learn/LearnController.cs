using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using One77.Api.WebApi48.Auth;

namespace One77.Api.WebApi48.Learn
{
    [RoutePrefix("api")]
    public sealed class LearnController : ApiController
    {
        // ---------- Public ----------

        [Route("learn/categories")]
        [HttpGet]
        public async Task<IHttpActionResult> GetPublicCategories()
        {
            var items = await CompositionRoot.LearnCategoryService.GetPublicCategoriesAsync();
            return Ok(items.Select(PublicLearnCategory.From));
        }

        [Route("learn/categories/{slug}")]
        [HttpGet]
        public async Task<IHttpActionResult> GetPublicCategoryDetail(string slug)
        {
            var detail = await CompositionRoot.LearnCategoryService.GetPublicCategoryDetailAsync(slug);
            return Ok(new PublicLearnCategoryDetail
            {
                Category = PublicLearnCategory.From(detail.Category),
                Entries = detail.Entries.Select(PublicLearnEntry.From).ToList()
            });
        }

        // ---------- Admin: categories ----------

        [Route("admin/learn/categories")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAdminCategories()
        {
            var items = await CompositionRoot.LearnCategoryService.GetAdminCategoriesAsync();
            return Ok(new { items, count = items.Count });
        }

        [Route("admin/learn/categories/{id:int}")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAdminCategory(int id) =>
            Ok(await CompositionRoot.LearnCategoryService.GetByIdAsync(id));

        [Route("admin/learn/categories")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> CreateCategory(CreateLearnCategoryRequest req) =>
            Ok(await CompositionRoot.LearnCategoryService.CreateAsync(req.Name, req.Slug, req.Description, req.SortPriority, req.Status));

        [Route("admin/learn/categories/{id:int}")]
        [HttpPut]
        [JwtAuthorize]
        public async Task<IHttpActionResult> UpdateCategory(int id, UpdateLearnCategoryRequest req) =>
            Ok(await CompositionRoot.LearnCategoryService.UpdateAsync(id, req.Name, req.Slug, req.Description, req.SortPriority, req.Status));

        [Route("admin/learn/categories/{id:int}/publish")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> PublishCategory(int id) =>
            Ok(await CompositionRoot.LearnCategoryService.PublishAsync(id));

        [Route("admin/learn/categories/{id:int}/archive")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> ArchiveCategory(int id) =>
            Ok(await CompositionRoot.LearnCategoryService.ArchiveAsync(id));

        [Route("admin/learn/categories/{id:int}/unarchive")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> UnarchiveCategory(int id) =>
            Ok(await CompositionRoot.LearnCategoryService.UnarchiveAsync(id));

        // ---------- Admin: entries ----------

        [Route("admin/learn/entries")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAdminEntries([FromUri(Name = "category_id")] int? categoryId)
        {
            var items = await CompositionRoot.LearnEntryService.GetAdminEntriesAsync(categoryId);
            return Ok(new { items, count = items.Count });
        }

        [Route("admin/learn/entries/{id:int}")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAdminEntry(int id) =>
            Ok(await CompositionRoot.LearnEntryService.GetByIdAsync(id));

        [Route("admin/learn/entries")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> CreateEntry(CreateLearnEntryRequest req) =>
            Ok(await CompositionRoot.LearnEntryService.CreateAsync(req.CategoryId, req.Title, req.Body, req.SortPriority, req.Status));

        [Route("admin/learn/entries/{id:int}")]
        [HttpPut]
        [JwtAuthorize]
        public async Task<IHttpActionResult> UpdateEntry(int id, UpdateLearnEntryRequest req) =>
            Ok(await CompositionRoot.LearnEntryService.UpdateAsync(id, req.CategoryId, req.Title, req.Body, req.SortPriority, req.Status));

        [Route("admin/learn/entries/{id:int}/publish")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> PublishEntry(int id) =>
            Ok(await CompositionRoot.LearnEntryService.PublishAsync(id));

        [Route("admin/learn/entries/{id:int}/archive")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> ArchiveEntry(int id) =>
            Ok(await CompositionRoot.LearnEntryService.ArchiveAsync(id));

        [Route("admin/learn/entries/{id:int}/unarchive")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> UnarchiveEntry(int id) =>
            Ok(await CompositionRoot.LearnEntryService.UnarchiveAsync(id));
    }
}
