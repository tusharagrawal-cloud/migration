namespace One77.Api.WebApi48.Homepage
{
    /// <summary>Shared shape for both the public and admin homepage-config reads — there is nothing admin-only to hide here beyond auth on the write endpoints themselves.</summary>
    public sealed class PublicHomepageConfig
    {
        public string HeroImageUrl { get; set; }
    }
}
