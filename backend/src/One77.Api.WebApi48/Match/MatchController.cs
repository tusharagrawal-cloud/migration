using System.Threading.Tasks;
using System.Web.Http;
using One77.Api.WebApi48.Auth;

namespace One77.Api.WebApi48.Match
{
    // ============================================================================
    // Every Shopify Product ID here is a query parameter or request-body field,
    // never a path segment — carried forward from One77.Api.DevHost's
    // MatchEndpoints.cs, where a real bug was found and fixed: canonical
    // Shopify GIDs contain literal "/" characters, and ASP.NET Core's router
    // does not reliably decode a percent-encoded "%2F" back to "/" inside a
    // {routeParam} path segment. Classic ASP.NET routing (System.Web.Routing,
    // which Web API 2 also builds on) has the same underlying URL-segment
    // decoding behavior, so this rule is preserved here for the same reason,
    // not merely for contract-parity's sake. See MATCH_PARITY_REPORT.md.
    // ============================================================================
    [RoutePrefix("api")]
    public sealed class MatchController : ApiController
    {
        // ---------- Public ----------

        [Route("products/matches")]
        [HttpGet]
        public async Task<IHttpActionResult> GetPublicMatches(
            [FromUri(Name = "shopify_product_id")] string shopifyProductId,
            [FromUri(Name = "use_case")] string useCase) =>
            Ok(await CompositionRoot.MatchPublicResolver.ResolveAsync(shopifyProductId, useCase));

        // ---------- Admin ----------

        [Route("admin/match/airguns")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetAirguns()
        {
            var items = await CompositionRoot.MatchAdminService.GetAirgunsAsync();
            return Ok(new { items, count = items.Count });
        }

        [Route("admin/match/relationships")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetRelationships(
            [FromUri(Name = "shopify_product_id")] string shopifyProductId,
            [FromUri(Name = "target_category")] string targetCategory)
        {
            var items = await CompositionRoot.MatchAdminService.GetRelationshipsAsync(shopifyProductId, targetCategory);
            return Ok(new { items, count = items.Count });
        }

        [Route("admin/match/candidates")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetCandidates(
            [FromUri(Name = "shopify_product_id")] string shopifyProductId,
            [FromUri(Name = "target_category")] string targetCategory,
            [FromUri(Name = "calibre")] string calibre,
            [FromUri(Name = "weight")] decimal? weight)
        {
            var items = await CompositionRoot.MatchAdminService.GetCandidatesAsync(shopifyProductId, targetCategory, calibre, weight);
            return Ok(new { items, count = items.Count });
        }

        [Route("admin/match/relationships")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> UpsertRelationship(UpsertMatchRelationshipRequest req) =>
            Ok(await CompositionRoot.MatchAdminService.UpsertRelationshipAsync(
                req.SourceShopifyProductId, req.TargetShopifyProductId, req.Status,
                req.Priority, req.UseCases, req.Reason, req.AdminNotes, req.CalibreOverride));

        [Route("admin/match/relationships/{id:int}")]
        [HttpPatch]
        [JwtAuthorize]
        public async Task<IHttpActionResult> PatchRelationship(int id, PatchMatchRelationshipRequest req) =>
            Ok(await CompositionRoot.MatchAdminService.PatchRelationshipAsync(
                id, req.Status, req.Priority, req.UseCases, req.Reason, req.AdminNotes, req.CalibreOverride, req.Active));

        [Route("admin/match/relationships/{id:int}")]
        [HttpDelete]
        [JwtAuthorize]
        public async Task<IHttpActionResult> DeleteRelationship(int id)
        {
            await CompositionRoot.MatchAdminService.DeleteRelationshipAsync(id);
            return Ok(new { ok = true });
        }

        [Route("admin/match/bulk")]
        [HttpPost]
        [JwtAuthorize]
        public async Task<IHttpActionResult> BulkMark(BulkMatchRequest req) =>
            Ok(await CompositionRoot.MatchAdminService.BulkMarkAsync(req.SourceShopifyProductId, req.TargetShopifyProductIds, req.Status));

        [Route("admin/match/summary")]
        [HttpGet]
        [JwtAuthorize]
        public async Task<IHttpActionResult> GetSummary([FromUri(Name = "shopify_product_id")] string shopifyProductId) =>
            Ok(await CompositionRoot.MatchAdminService.GetCompletenessAsync(shopifyProductId));
    }
}
