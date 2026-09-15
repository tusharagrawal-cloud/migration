using Microsoft.AspNetCore.Mvc;
using One77.Core.Bundles;

namespace One77.Api.DevHost.Bundles;

// ============================================================================
// DEV-ONLY: none of the /api/admin/bundles/* endpoints below carry any
// authentication or authorization — same explicit boundary as Learn/Webinar/
// Match/Enrichment.
// ============================================================================
public static class BundleEndpoints
{
    public static void MapBundleEndpoints(this WebApplication app)
    {
        app.MapGet("/api/bundles", async (BundleService service) =>
        {
            var bundles = await service.GetPublicBundlesAsync();
            return Results.Ok(bundles.Select(ToPublicBundle));
        });

        app.MapGet("/api/admin/bundles", async (
            [FromQuery(Name = "status")] string? status,
            [FromQuery(Name = "q")] string? q,
            BundleService service) =>
        {
            var items = await service.GetAdminBundlesAsync(status, q);
            return Results.Ok(new { items, count = items.Count });
        });

        app.MapGet("/api/admin/bundles/{id:int}", async (int id, BundleService service) =>
            Results.Ok(await service.GetByIdAsync(id)));

        app.MapPost("/api/admin/bundles", async (CreateBundleRequest req, BundleService service) =>
            Results.Ok(await service.CreateAsync(req.Name, req.Tagline, req.Status, req.SortPriority, ToCoreItems(req.Items))));

        app.MapPut("/api/admin/bundles/{id:int}", async (int id, UpdateBundleRequest req, BundleService service) =>
            Results.Ok(await service.UpdateAsync(id, req.Name, req.Tagline, req.Status, req.SortPriority, ToCoreItems(req.Items))));

        app.MapPost("/api/admin/bundles/{id:int}/publish", async (int id, BundleService service) =>
            Results.Ok(await service.PublishAsync(id)));

        app.MapPost("/api/admin/bundles/{id:int}/archive", async (int id, BundleService service) =>
            Results.Ok(await service.ArchiveAsync(id)));

        app.MapPost("/api/admin/bundles/{id:int}/unarchive", async (int id, BundleService service) =>
            Results.Ok(await service.UnarchiveAsync(id)));
    }

    private static List<BundleItemSpec>? ToCoreItems(List<BundleItemInput>? items) =>
        items?.Select(i => new BundleItemSpec { ShopifyProductId = i.ShopifyProductId, ItemRole = i.ItemRole }).ToList();

    private static PublicBundle ToPublicBundle(Bundle b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Tagline = b.Tagline ?? "",
        SortPriority = b.SortPriority,
        Items = b.Items.Select(i => new PublicBundleItem { ShopifyProductId = i.ShopifyProductId, ItemRole = i.ItemRole, SortOrder = i.SortOrder }).ToList()
    };
}
