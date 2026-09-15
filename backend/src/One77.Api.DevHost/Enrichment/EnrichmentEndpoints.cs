using Microsoft.AspNetCore.Mvc;
using One77.Core.Enrichment;

namespace One77.Api.DevHost.Enrichment;

// ============================================================================
// DEV-ONLY: none of the /api/admin/enrichment/* endpoints below carry any
// authentication or authorization — same explicit boundary as Learn/Webinar/
// Match. Every Shopify Product ID here is a query parameter or request-body
// field, never a path segment — see MATCH_PARITY_REPORT.md's "Bug found and
// fixed" section for why (GIDs contain literal "/").
// ============================================================================
public static class EnrichmentEndpoints
{
    public static void MapEnrichmentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/enrichment", async (EnrichmentService service) =>
        {
            var items = await service.GetAllAsync();
            return Results.Ok(new { items, count = items.Count });
        });

        app.MapGet("/api/admin/enrichment/by-product", async (
            [FromQuery(Name = "shopify_product_id")] string shopifyProductId,
            EnrichmentService service) =>
            Results.Ok(await service.GetByShopifyProductIdAsync(shopifyProductId)));

        app.MapPost("/api/admin/enrichment", async (CreateEnrichmentRequest req, EnrichmentService service) =>
            Results.Ok(await service.CreateAsync(
                req.ShopifyProductId, req.Category, req.IsActive,
                req.Calibre, req.PowerplantType, req.WeightGrains,
                req.RecommendedPelletWeightMin, req.RecommendedPelletWeightMax,
                req.CompatiblePowerplants, req.UseCases, ToCoreSpecs(req.Specifications))));

        app.MapPut("/api/admin/enrichment", async (UpdateEnrichmentRequest req, EnrichmentService service) =>
            Results.Ok(await service.UpdateAsync(
                req.ShopifyProductId, req.Category, req.IsActive,
                req.Calibre, req.PowerplantType, req.WeightGrains,
                req.RecommendedPelletWeightMin, req.RecommendedPelletWeightMax,
                req.CompatiblePowerplants, req.UseCases, ToCoreSpecs(req.Specifications))));

        // Public/internal read for the future Product Detail assembly layer.
        app.MapGet("/api/products/enrichment", async (
            [FromQuery(Name = "shopify_product_id")] string shopifyProductId,
            EnrichmentService service) =>
        {
            var record = await service.GetPublicByShopifyProductIdAsync(shopifyProductId);
            return Results.Ok(new PublicEnrichment
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
        });
    }

    private static List<EnrichmentSpecification>? ToCoreSpecs(List<SpecificationDto>? specs) =>
        specs?.Select(s => new EnrichmentSpecification { SpecKey = s.SpecKey, SpecValue = s.SpecValue, SortOrder = s.SortOrder }).ToList();
}
