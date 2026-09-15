using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;

namespace One77.Core.Admin
{
    /// <summary>
    /// AdminUsers data access. Same explicit, connection-per-call pattern as
    /// every other repository in this migration (WebinarRepository et al.).
    /// </summary>
    public sealed class AdminUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminUserRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string Columns =
            "AdminUserId, Email, PasswordHash, Name, IsActive, CreatedAt, UpdatedAt";

        public async Task<AdminUser> GetByEmailAsync(string email)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.QueryFirstOrDefaultAsync<AdminUser>(
                    "SELECT " + Columns + " FROM dbo.AdminUsers WHERE Email = @Email;",
                    new { Email = email });
            }
        }

        public async Task<AdminUser> GetByIdAsync(int id)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.QueryFirstOrDefaultAsync<AdminUser>(
                    "SELECT " + Columns + " FROM dbo.AdminUsers WHERE AdminUserId = @Id;",
                    new { Id = id });
            }
        }

        public async Task<bool> AnyExistsAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.AdminUsers;");
                return count > 0;
            }
        }

        public async Task<int> InsertAsync(string email, string passwordHash, string name)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.AdminUsers (Email, PasswordHash, Name)
                      OUTPUT INSERTED.AdminUserId
                      VALUES (@Email, @PasswordHash, @Name);",
                    new { Email = email, PasswordHash = passwordHash, Name = name });
            }
        }
    }
}
