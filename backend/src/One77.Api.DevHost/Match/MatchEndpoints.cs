using Microsoft.AspNetCore.Mvc;
using One77.Core.Match;

namespace One77.Api.DevHost.Match;

// ============================================================================
// DEV-ONLY: none of the /api/admin/match/* endpoints below carry any
// authentication or authorization — same explicit boundary as Learn and
// Webinar. Production Admin auth is reserved for the Section 11 checkpoint.
// ============================================================================
//
// ROUTING NOTE (real bug found during live HTTP verification, fixed here):
// canonical Shopify Product GIDs contain literal "/" characters
// (gid://shopify/Product/123...). ASP.NET Core's router does not reliably
// decode a percent-encoded "%2F" back to "/" when it appears inside a
// {routeParam} path segment — a request built with a properly-encoded GID
// silently resolved to an empty/wrong value instead of 404ing loudly, which
// only surfaced once these endpoints were exercised over real HTTP (the
// automated test suite calls the service layer directly and never hit this
// class of bug at all). Rather than fight Kestrel/routing configuration for
// an edge case invented purely by our own path design, every Shopify
// Product ID is passed as a query parameter instead — query-string parsing
// has no such restriction. Only the local integer relationship ID (never a
// GID) appears in a path segment. See MATCH_PARITY_REPORT.md.
// ============================================================================
public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this WebApplication app)
    {
        MapPublicEndpoints(app);
        MapAdminEndpoints(app);
    }

    private static void MapPublicEndpoints(WebApplication app)
    {
        app.MapGet("/api/products/matches", async (
            [FromQuery(Name = "shopify_product_id")] string shopifyProductId,
            [FromQuery(Name = "use_case")] string? useCase,
            MatchPublicResolver resolver) =>
            Results.Ok(await resolver.ResolveAsync(shopifyProductId, useCase)));
    }

    private static void MapAdminEndpoints(WebApplication app)
    {
        app.MapGet("/api/admin/match/airguns", async (MatchAdminService match) =>
        {
            var items = await match.GetAirgunsAsync();
            return Results.Ok(new { items, count = items.Count });
        });

        app.MapGet("/api/admin/match/relationships", async (
            [FromQuery(Name = "shopify_product_id")] string shopifyProductId,
            [FromQuery(Name = "target_category")] string? targetCategory,
            MatchAdminService match) =>
        {
            var items = await match.GetRelationshipsAsync(shopifyProductId, targetCategory);
            return Results.Ok(new { items, count = items.Count });
        });

        app.MapGet("/api/admin/match/candidates", async (
            [FromQuery(Name = "shopify_product_id")] string shopifyProductId,
            [FromQuery(Name = "target_category")] string targetCategory,
            [FromQuery(Name = "calibre")] string? calibre,
            [FromQuery(Name = "weight")] decimal? weight,
            MatchAdminService match) =>
        {
            var items = await match.GetCandidatesAsync(shopifyProductId, targetCategory, calibre, weight);
            return Results.Ok(new { items, count = items.Count });
        });

        app.MapPost("/api/admin/match/relationships", async (UpsertMatchRelationshipRequest req, MatchAdminService match) =>
            Results.Ok(await match.UpsertRelationshipAsync(
                req.SourceShopifyProductId, req.TargetShopifyProductId, req.Status,
                req.Priority, req.UseCases, req.Reason, req.AdminNotes, req.CalibreOverride)));

        app.MapPatch("/api/admin/match/relationships/{id:int}", async (int id, PatchMatchRelationshipRequest req, MatchAdminService match) =>
            Results.Ok(await match.PatchRelationshipAsync(
                id, req.Status, req.Priority, req.UseCases, req.Reason, req.AdminNotes, req.CalibreOverride, req.Active)));

        app.MapDelete("/api/admin/match/relationships/{id:int}", async (int id, MatchAdminService match) =>
        {
            await match.DeleteRelationshipAsync(id);
            return Results.Ok(new { ok = true });
        });

        app.MapPost("/api/admin/match/bulk", async (BulkMatchRequest req, MatchAdminService match) =>
            Results.Ok(await match.BulkMarkAsync(req.SourceShopifyProductId, req.TargetShopifyProductIds, req.Status)));

        app.MapGet("/api/admin/match/summary", async (
            [FromQuery(Name = "shopify_product_id")] string shopifyProductId,
            MatchAdminService match) =>
            Results.Ok(await match.GetCompletenessAsync(shopifyProductId)));
    }
}
