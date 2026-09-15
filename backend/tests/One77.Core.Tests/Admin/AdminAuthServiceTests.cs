using System.Threading.Tasks;
using One77.Core.Admin;
using Xunit;

namespace One77.Core.Tests.Admin
{
    public class AdminAuthServiceTests : AdminAuthTestBase
    {
        [Fact]
        public async Task Login_WithValidCredentials_ReturnsAdmin()
        {
            if (!HasDatabase) { return; }

            var repository = CreateRepository();
            var service = CreateService(repository);
            var email = UniqueEmail();
            var id = await repository.InsertAsync(email, new BCryptPasswordHasher().Hash("correct-horse"), "Test Admin");

            try
            {
                var admin = await service.LoginAsync(email, "correct-horse");

                Assert.Equal(id, admin.AdminUserId);
                Assert.Equal(email, admin.Email);
                Assert.True(admin.IsActive);
            }
            finally
            {
                await DeleteAdminAsync(id);
            }
        }

        [Fact]
        public async Task Login_WithUnknownEmail_ThrowsGenericAuthenticationException()
        {
            if (!HasDatabase) { return; }

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<AdminAuthenticationException>(
                () => service.LoginAsync(UniqueEmail(), "whatever"));
            Assert.Equal("Incorrect email or password.", ex.Message);
        }

        [Fact]
        public async Task Login_WithWrongPassword_ThrowsSameGenericException()
        {
            if (!HasDatabase) { return; }

            var repository = CreateRepository();
            var service = CreateService(repository);
            var email = UniqueEmail();
            var id = await repository.InsertAsync(email, new BCryptPasswordHasher().Hash("correct-horse"), "Test Admin");

            try
            {
                var ex = await Assert.ThrowsAsync<AdminAuthenticationException>(
                    () => service.LoginAsync(email, "wrong-password"));
                // Same message as unknown-email — must never reveal which case applied.
                Assert.Equal("Incorrect email or password.", ex.Message);
            }
            finally
            {
                await DeleteAdminAsync(id);
            }
        }

        [Fact]
        public async Task Login_WithInactiveAdmin_IsRejected()
        {
            if (!HasDatabase) { return; }

            var repository = CreateRepository();
            var service = CreateService(repository);
            var email = UniqueEmail();
            var id = await repository.InsertAsync(email, new BCryptPasswordHasher().Hash("correct-horse"), "Test Admin");

            try
            {
                using var connection = await OpenRawAsync();
                await Dapper.SqlMapper.ExecuteAsync(connection,
                    "UPDATE dbo.AdminUsers SET IsActive = 0 WHERE AdminUserId = @Id;", new { Id = id });

                var ex = await Assert.ThrowsAsync<AdminAuthenticationException>(
                    () => service.LoginAsync(email, "correct-horse"));
                Assert.Equal("Incorrect email or password.", ex.Message);
            }
            finally
            {
                await DeleteAdminAsync(id);
            }
        }

        [Fact]
        public async Task PasswordHash_IsNeverStoredPlaintext()
        {
            if (!HasDatabase) { return; }

            var repository = CreateRepository();
            var email = UniqueEmail();
            const string rawPassword = "correct-horse-battery-staple";
            var id = await repository.InsertAsync(email, new BCryptPasswordHasher().Hash(rawPassword), "Test Admin");

            try
            {
                var stored = await repository.GetByEmailAsync(email);
                Assert.NotEqual(rawPassword, stored!.PasswordHash);
                // BCrypt's own format marker — confirms a real BCrypt hash was stored, not a raw/reversible value.
                Assert.StartsWith("$2", stored.PasswordHash);
            }
            finally
            {
                await DeleteAdminAsync(id);
            }
        }

        [Fact]
        public async Task EnsureSeedAdmin_CreatesFirstAdmin_WhenTableIsEmpty()
        {
            if (!HasDatabase) { return; }

            await DeleteAllAdminsAsync();
            var repository = CreateRepository();
            var service = CreateService(repository);
            var email = UniqueEmail();

            try
            {
                await service.EnsureSeedAdminAsync(email, "initial-password", "Initial Admin");

                var created = await repository.GetByEmailAsync(email);
                Assert.NotNull(created);
                Assert.True(created!.IsActive);

                // The seed password must never be stored as plaintext — confirm the stored
                // value is a real BCrypt hash (round-trips through Verify) and not the raw input.
                Assert.NotEqual("initial-password", created.PasswordHash);
                Assert.True(BCrypt.Net.BCrypt.Verify("initial-password", created.PasswordHash));
            }
            finally
            {
                await DeleteAllAdminsAsync();
            }
        }

        [Fact]
        public async Task EnsureSeedAdmin_DoesNothing_WhenAnAdminAlreadyExists()
        {
            if (!HasDatabase) { return; }

            var repository = CreateRepository();
            var service = CreateService(repository);
            var existingEmail = UniqueEmail();
            var existingId = await repository.InsertAsync(existingEmail, new BCryptPasswordHasher().Hash("x"), "Existing Admin");

            try
            {
                var candidateEmail = UniqueEmail();
                await service.EnsureSeedAdminAsync(candidateEmail, "should-not-be-created", "Should Not Exist");

                var notCreated = await repository.GetByEmailAsync(candidateEmail);
                Assert.Null(notCreated);
            }
            finally
            {
                await DeleteAdminAsync(existingId);
            }
        }

        [Fact]
        public async Task GetById_ReturnsMatchingAdmin()
        {
            if (!HasDatabase) { return; }

            var repository = CreateRepository();
            var service = CreateService(repository);
            var email = UniqueEmail();
            var id = await repository.InsertAsync(email, new BCryptPasswordHasher().Hash("x"), "Test Admin");

            try
            {
                var admin = await service.GetByIdAsync(id);
                Assert.Equal(email, admin.Email);
            }
            finally
            {
                await DeleteAdminAsync(id);
            }
        }
    }
}
