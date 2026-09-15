using System;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;
using One77.Core.Homepage;

namespace One77.Core.Tests.Homepage
{
    /// <summary>
    /// Shared base for Homepage integration tests. Exercises the real
    /// HomepageConfigRepository/HomepageConfigService against the REAL SQL
    /// Server 2019 instance named by ONE77_TEST_CONNECTION_STRING — never a
    /// mock — plus real local-disk storage under a fresh temp directory per
    /// test, cleaned up afterward. The dbo.HomepageConfig row is a shared
    /// singleton, so every test resets it back to its empty state when done.
    /// </summary>
    public abstract class HomepageTestBase : IDisposable
    {
        protected static string? ConnectionString =>
            Environment.GetEnvironmentVariable("ONE77_TEST_CONNECTION_STRING");

        protected static bool HasDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

        protected readonly string MediaRoot;

        protected HomepageTestBase()
        {
            MediaRoot = Path.Combine(Path.GetTempPath(), "one77-homepage-tests-" + Guid.NewGuid().ToString("N"));
        }

        protected HomepageConfigRepository CreateRepository() =>
            new HomepageConfigRepository(new SqlConnectionFactory(ConnectionString!));

        protected HomepageMediaStorage CreateStorage(long maxBytes = 8 * 1024 * 1024) =>
            new HomepageMediaStorage(MediaRoot, "/media", maxBytes);

        protected HomepageConfigService CreateService(HomepageConfigRepository? repository = null, HomepageMediaStorage? storage = null) =>
            new HomepageConfigService(repository ?? CreateRepository(), storage ?? CreateStorage());

        // Recognizable minimal valid file signatures — enough bytes for
        // HomepageMediaStorage's own sniffing, not a decodable image (this
        // storage layer never decodes pixels, only checks the signature).
        protected static byte[] MinimalJpegBytes() => new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        protected static byte[] MinimalPngBytes() => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
        protected static byte[] NotAnImageBytes() => System.Text.Encoding.UTF8.GetBytes("this is definitely not an image");

        protected static async Task ResetSingletonRowAsync()
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("UPDATE dbo.HomepageConfig SET HeroImagePath = NULL, UpdatedAt = SYSUTCDATETIME() WHERE Id = 1;");
        }

        public void Dispose()
        {
            if (Directory.Exists(MediaRoot))
            {
                Directory.Delete(MediaRoot, recursive: true);
            }
        }
    }
}
