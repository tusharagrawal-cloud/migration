using One77.Core.Homepage;

namespace One77.Api.DevHost.Homepage;

public static class HomepageEndpoints
{
    public static void MapHomepageEndpoints(this WebApplication app)
    {
        app.MapGet("/api/homepage", async (HomepageConfigService service) =>
            Results.Ok(ToPublicConfig(await service.GetAsync())));

        app.MapGet("/api/admin/homepage", async (HomepageConfigService service) =>
            Results.Ok(ToPublicConfig(await service.GetAsync())));

        app.MapPost("/api/admin/homepage/hero-image", async (HttpRequest request, HomepageConfigService service) =>
        {
            if (!request.HasFormContentType)
            {
                throw new HomepageValidationException("Please choose an image to upload.");
            }

            var form = await request.ReadFormAsync();
            var file = form.Files.Count > 0 ? form.Files[0] : null;
            if (file == null || file.Length == 0)
            {
                throw new HomepageValidationException("Please choose an image to upload.");
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var config = await service.SetHeroImageAsync(stream.ToArray());
            return Results.Ok(ToPublicConfig(config));
        });

        app.MapDelete("/api/admin/homepage/hero-image", async (HomepageConfigService service) =>
            Results.Ok(ToPublicConfig(await service.RemoveHeroImageAsync())));
    }

    private static PublicHomepageConfig ToPublicConfig(HomepageConfig config) => new()
    {
        HeroImageUrl = config.HeroImagePath
    };
}
