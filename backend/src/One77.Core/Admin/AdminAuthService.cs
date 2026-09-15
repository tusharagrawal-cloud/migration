using System.Threading.Tasks;

namespace One77.Core.Admin
{
    /// <summary>
    /// Admin login business logic — deliberately host-agnostic (no HTTP, no
    /// JWT here; JWT issuance is a production-host concern, wired by the
    /// caller from the AdminUser this returns). V1 is single/small-admin:
    /// no roles, no lockout policy, no password-reset flow.
    /// </summary>
    public sealed class AdminAuthService
    {
        private const string GenericLoginFailureMessage = "Incorrect email or password.";

        private readonly AdminUserRepository _repository;
        private readonly IPasswordHasher _passwordHasher;

        public AdminAuthService(AdminUserRepository repository, IPasswordHasher passwordHasher)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
        }

        /// <summary>
        /// Validates credentials and returns the matching active admin.
        /// Throws the same <see cref="AdminAuthenticationException"/> with
        /// the same generic message for a missing email, a wrong password,
        /// and a disabled account — never reveals which case applied.
        /// </summary>
        public async Task<AdminUser> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new AdminAuthenticationException(GenericLoginFailureMessage);
            }

            var admin = await _repository.GetByEmailAsync(email.Trim());
            if (admin == null || !admin.IsActive || !_passwordHasher.Verify(password, admin.PasswordHash))
            {
                throw new AdminAuthenticationException(GenericLoginFailureMessage);
            }

            return admin;
        }

        /// <summary>Used by the "/me" endpoint to re-resolve the admin identified by a validated JWT.</summary>
        public Task<AdminUser> GetByIdAsync(int adminUserId)
        {
            return _repository.GetByIdAsync(adminUserId);
        }

        /// <summary>
        /// Creates the first admin account from configuration-supplied
        /// credentials — but only if no admin exists yet. Safe to call on
        /// every application start: a no-op once any admin has been
        /// created, so it never re-hashes or overwrites an existing
        /// account's credentials on subsequent boots.
        /// </summary>
        public async Task EnsureSeedAdminAsync(string email, string password, string name)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            if (await _repository.AnyExistsAsync())
            {
                return;
            }

            var hash = _passwordHasher.Hash(password);
            await _repository.InsertAsync(email.Trim(), hash, string.IsNullOrWhiteSpace(name) ? "Admin" : name);
        }
    }
}
