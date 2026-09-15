using System;
using System.Threading.Tasks;
using One77.Core.Data;
using One77.Core.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests
{
    /// <summary>
    /// Genuine integration test against a real SQL Server instance.
    ///
    /// Reads its connection string from the ONE77_TEST_CONNECTION_STRING
    /// environment variable rather than a committed file, so no credential
    /// of any kind — even a local-dev one — ever lives in source control.
    ///
    /// If the variable is not set (e.g. a machine with no SQL Server
    /// available), the test body exits early rather than failing the whole
    /// suite — xUnit 2.4.x has no built-in dynamic-skip API without an
    /// extension package, so this is a deliberate, documented compromise:
    /// treat an early return in this class as "not exercised in this run",
    /// never as "verified passing".
    /// </summary>
    public class DatabaseConnectivityIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public DatabaseConnectivityIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanConnectAsync_ReturnsTrue_AgainstRealSqlServer()
        {
            var connectionString = Environment.GetEnvironmentVariable("ONE77_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _output.WriteLine("SKIPPED (not executed against a real database): ONE77_TEST_CONNECTION_STRING is not set.");
                return;
            }

            var factory = new SqlConnectionFactory(connectionString);
            var checker = new DatabaseConnectivityChecker(factory);

            var connected = await checker.CanConnectAsync();

            Assert.True(connected, "Expected SELECT 1 to succeed against the configured SQL Server instance.");
        }
    }
}
