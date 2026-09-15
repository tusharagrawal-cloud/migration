using Microsoft.AspNetCore.Mvc;
using One77.Core.Learn;

namespace One77.Api.DevHost.Learn;

// ============================================================================
// DEV-ONLY: none of the /api/admin/learn/* endpoints below carry any
// authentication or authorization. Production Admin auth is a decision
// reserved for the Phase 3 Section 11 checkpoint (a later milestone) and is
// intentionally NOT implemented here. This is a real, honest gap — not a
// stubbed-out bypass dressed up as security — and must not be mistaken for
// production-ready behavior or deployed as-is.
// ============================================================================
public static class LearnEndpoints
{
    public static void MapLearnEndpoints(this WebApplication app)
    {
        MapPublicEndpoints(app);
        MapAdminCategoryEndpoints(app);
        MapAdminEntryEndpoints(app);
    }

    private static void MapPublicEndpoints(WebApplication app)
    {
        app.MapGet("/api/learn/categories", async (LearnCategoryService categories) =>
        {
            var items = await categories.GetPublicCategoriesAsync();
            return Results.Ok(items.Select(ToPublicCategory));
        });

        app.MapGet("/api/learn/categories/{slug}", async (string slug, LearnCategoryService categories) =>
        {
            var detail = await categories.GetPublicCategoryDetailAsync(slug);
            return Results.Ok(new PublicLearnCategoryDetail
            {
                Category = ToPublicCategory(detail.Category),
                Entries = detail.Entries.Select(ToPublicEntry).ToList()
            });
        });
    }

    private static void MapAdminCategoryEndpoints(WebApplication app)
    {
        app.MapGet("/api/admin/learn/categories", async (LearnCategoryService categories) =>
        {
            var items = await categories.GetAdminCategoriesAsync();
            return Results.Ok(new { items, count = items.Count });
        });

        app.MapGet("/api/admin/learn/categories/{id:int}", async (int id, LearnCategoryService categories) =>
            Results.Ok(await categories.GetByIdAsync(id)));

        app.MapPost("/api/admin/learn/categories", async (CreateLearnCategoryRequest req, LearnCategoryService categories) =>
            Results.Ok(await categories.CreateAsync(req.Name, req.Slug, req.Description, req.SortPriority, req.Status)));

        app.MapPut("/api/admin/learn/categories/{id:int}", async (int id, UpdateLearnCategoryRequest req, LearnCategoryService categories) =>
            Results.Ok(await categories.UpdateAsync(id, req.Name, req.Slug, req.Description, req.SortPriority, req.Status)));

        app.MapPost("/api/admin/learn/categories/{id:int}/publish", async (int id, LearnCategoryService categories) =>
            Results.Ok(await categories.PublishAsync(id)));

        app.MapPost("/api/admin/learn/categories/{id:int}/archive", async (int id, LearnCategoryService categories) =>
            Results.Ok(await categories.ArchiveAsync(id)));

        app.MapPost("/api/admin/learn/categories/{id:int}/unarchive", async (int id, LearnCategoryService categories) =>
            Results.Ok(await categories.UnarchiveAsync(id)));
    }

    private static void MapAdminEntryEndpoints(WebApplication app)
    {
        app.MapGet("/api/admin/learn/entries", async ([FromQuery(Name = "category_id")] int? categoryId, LearnEntryService entries) =>
        {
            var items = await entries.GetAdminEntriesAsync(categoryId);
            return Results.Ok(new { items, count = items.Count });
        });

        app.MapGet("/api/admin/learn/entries/{id:int}", async (int id, LearnEntryService entries) =>
            Results.Ok(await entries.GetByIdAsync(id)));

        app.MapPost("/api/admin/learn/entries", async (CreateLearnEntryRequest req, LearnEntryService entries) =>
            Results.Ok(await entries.CreateAsync(req.CategoryId, req.Title, req.Body, req.SortPriority, req.Status)));

        app.MapPut("/api/admin/learn/entries/{id:int}", async (int id, UpdateLearnEntryRequest req, LearnEntryService entries) =>
            Results.Ok(await entries.UpdateAsync(id, req.CategoryId, req.Title, req.Body, req.SortPriority, req.Status)));

        app.MapPost("/api/admin/learn/entries/{id:int}/publish", async (int id, LearnEntryService entries) =>
            Results.Ok(await entries.PublishAsync(id)));

        app.MapPost("/api/admin/learn/entries/{id:int}/archive", async (int id, LearnEntryService entries) =>
            Results.Ok(await entries.ArchiveAsync(id)));

        app.MapPost("/api/admin/learn/entries/{id:int}/unarchive", async (int id, LearnEntryService entries) =>
            Results.Ok(await entries.UnarchiveAsync(id)));
    }

    private static PublicLearnCategory ToPublicCategory(LearnCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        Description = c.Description
    };

    private static PublicLearnEntry ToPublicEntry(LearnEntry e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Body = e.Body
    };
}
