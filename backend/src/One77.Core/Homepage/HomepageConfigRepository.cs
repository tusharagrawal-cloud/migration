using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;

namespace One77.Core.Homepage
{
    /// <summary>
    /// Data access for the single dbo.HomepageConfig row (Id = 1, seeded by
    /// 007_homepage.sql at schema-deploy time — there is always exactly one
    /// row, so every method here targets it directly with no "find or
    /// create" branching).
    /// </summary>
    public sealed class HomepageConfigRepository
    {
        private const int SingletonId = 1;
        private readonly IDbConnectionFactory _connectionFactory;

        public HomepageConfigRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<HomepageConfig> GetAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.QuerySingleAsync<HomepageConfig>(
                    "SELECT Id, HeroImagePath, UpdatedAt FROM dbo.HomepageConfig WHERE Id = @Id;",
                    new { Id = SingletonId });
            }
        }

        public async Task SetHeroImagePathAsync(string heroImagePath)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    "UPDATE dbo.HomepageConfig SET HeroImagePath = @HeroImagePath, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;",
                    new { Id = SingletonId, HeroImagePath = heroImagePath });
            }
        }
    }
}
