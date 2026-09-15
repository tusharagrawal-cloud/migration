namespace One77.Api.DevHost.Homepage;

// Shared shape for both the public and admin homepage-config reads — there
// is nothing admin-only to hide here beyond auth on the write endpoints
// themselves.
public sealed class PublicHomepageConfig
{
    public string? HeroImageUrl { get; set; }
}
