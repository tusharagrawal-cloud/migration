using One77.Core.Admin;
using One77.Core.Bundles;
using One77.Core.Data;
using One77.Core.Enrichment;
using One77.Core.Homepage;
using One77.Core.Learn;
using One77.Core.Match;
using One77.Core.Webinar;

namespace One77.Api.WebApi48
{
    /// <summary>
    /// Manual dependency composition — no DI container. Every service here
    /// is stateless beyond its connection factory (same lifetime model as
    /// the AddSingleton registrations in One77.Api.DevHost's Program.cs),
    /// so plain static singletons, built once, are sufficient and keep this
    /// host's own footprint to "the smallest supported configuration
    /// necessary" rather than adding a DI framework for its own sake.
    /// </summary>
    public static class CompositionRoot
    {
        public static readonly IDbConnectionFactory ConnectionFactory =
            new SqlConnectionFactory(AppConfig.SqlConnectionString);

        public static readonly LearnRepository LearnRepository = new LearnRepository(ConnectionFactory);
        public static readonly LearnCategoryService LearnCategoryService = new LearnCategoryService(LearnRepository);
        public static readonly LearnEntryService LearnEntryService = new LearnEntryService(LearnRepository);

        public static readonly WebinarRepository WebinarRepository = new WebinarRepository(ConnectionFactory);
        public static readonly WebinarEventService WebinarEventService = new WebinarEventService(WebinarRepository);
        public static readonly WebinarRegistrationService WebinarRegistrationService = new WebinarRegistrationService(WebinarRepository);

        public static readonly MatchRepository MatchRepository = new MatchRepository(ConnectionFactory);
        public static readonly MatchAdminService MatchAdminService = new MatchAdminService(MatchRepository);
        public static readonly MatchPublicResolver MatchPublicResolver = new MatchPublicResolver(MatchRepository);

        public static readonly EnrichmentRepository EnrichmentRepository = new EnrichmentRepository(ConnectionFactory);
        public static readonly EnrichmentService EnrichmentService = new EnrichmentService(EnrichmentRepository);

        public static readonly BundleRepository BundleRepository = new BundleRepository(ConnectionFactory);
        public static readonly BundleService BundleService = new BundleService(BundleRepository);

        public static readonly AdminUserRepository AdminUserRepository = new AdminUserRepository(ConnectionFactory);
        public static readonly AdminAuthService AdminAuthService = new AdminAuthService(AdminUserRepository, new BCryptPasswordHasher());

        public static readonly HomepageConfigRepository HomepageConfigRepository = new HomepageConfigRepository(ConnectionFactory);
        public static readonly HomepageMediaStorage HomepageMediaStorage = new HomepageMediaStorage(
            AppConfig.HomepageMediaPhysicalRoot, AppConfig.HomepageMediaUrlPrefix, AppConfig.HomepageMediaMaxBytes);
        public static readonly HomepageConfigService HomepageConfigService = new HomepageConfigService(HomepageConfigRepository, HomepageMediaStorage);

        public static readonly JwtTokenService JwtTokenService = new JwtTokenService(new JwtOptions
        {
            SigningSecret = AppConfig.JwtSigningSecret,
            Issuer = AppConfig.JwtIssuer,
            Audience = AppConfig.JwtAudience,
            ExpiresAfter = AppConfig.JwtExpiry
        });
    }
}
