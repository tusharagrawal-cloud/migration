using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using One77.Api.WebApi48.Auth;
using One77.Core.Enrichment;

namespace One77.Api.WebApi48.Enrichment
{
    [RoutePrefix("api")]
    public sealed class EnrichmentController : ApiController
    {
        [Route("admin/enrichment")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAll()
        {
            var items = await CompositionRoot.EnrichmentService.GetAllAsync();
            return Ok(new { items, count = items.Count });
        }

        [Route("admin/enrichment/by-product")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetByProduct([FromUri(Name = "shopify_product_id")] string shopifyProductId) =>
            Ok(await CompositionRoot.EnrichmentService.GetByShopifyProductIdAsync(shopifyProductId));

        [Route("admin/enrichment")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> Create(CreateEnrichmentRequest req) =>
            Ok(await CompositionRoot.EnrichmentService.CreateAsync(
                req.ShopifyProductId, req.Category, req.IsActive,
                req.Calibre, req.PowerplantType, req.WeightGrains,
                req.RecommendedPelletWeightMin, req.RecommendedPelletWeightMax,
                req.CompatiblePowerplants, req.UseCases, ToCoreSpecs(req.Specifications)));

        [Route("admin/enrichment")]
        [HttpPut]
        [JwtAuthorize]
        public async Task<IHttpActionResult> Update(UpdateEnrichmentRequest req) =>
            Ok(await CompositionRoot.EnrichmentService.UpdateAsync(
                req.ShopifyProductId, req.Category, req.IsActive,
                req.Calibre, req.PowerplantType, req.WeightGrains,
                req.RecommendedPelletWeightMin, req.RecommendedPelletWeightMax,
                req.CompatiblePowerplants, req.UseCases, ToCoreSpecs(req.Specifications)));

        // Public/internal read for Product Detail.
        [Route("products/enrichment")]
        [HttpGet]
        public async Task<IHttpActionResult> GetPublic([FromUri(Name = "shopify_product_id")] string shopifyProductId)
        {
            var record = await CompositionRoot.EnrichmentService.GetPublicByShopifyProductIdAsync(shopifyProductId);
            return Ok(new PublicEnrichment
            {
                ShopifyProductId = record.ShopifyProductId,
                Category = record.Category,
                Calibre = record.Calibre,
                PowerplantType = record.PowerplantType,
                WeightGrains = record.WeightGrains,
                RecommendedPelletWeightMin = record.RecommendedPelletWeightMin,
                RecommendedPelletWeightMax = record.RecommendedPelletWeightMax,
                CompatiblePowerplants = record.CompatiblePowerplants,
                UseCases = record.UseCases,
                Specifications = record.Specifications.Select(s => new SpecificationDto { SpecKey = s.SpecKey, SpecValue = s.SpecValue, SortOrder = s.SortOrder }).ToList()
            });
        }

        private static System.Collections.Generic.List<EnrichmentSpecification> ToCoreSpecs(System.Collections.Generic.List<SpecificationDto> specs) =>
            specs?.Select(s => new EnrichmentSpecification { SpecKey = s.SpecKey, SpecValue = s.SpecValue, SortOrder = s.SortOrder }).ToList();
    }
}
