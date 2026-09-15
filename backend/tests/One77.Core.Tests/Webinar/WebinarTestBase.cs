using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using One77.Core.Data;
using One77.Core.Webinar;

namespace One77.Core.Tests.Webinar
{
    /// <summary>
    /// Shared base for Milestone 5 Webinar integration tests. Exercises the
    /// real WebinarRepository/WebinarEventService/WebinarRegistrationService
    /// classes against the REAL SQL Server 2019 instance named by
    /// ONE77_TEST_CONNECTION_STRING — never a mock. If the variable is
    /// absent, tests report "not executed" via test output rather than
    /// silently passing.
    /// </summary>
    public abstract class WebinarTestBase
    {
        protected static string? ConnectionString =>
            Environment.GetEnvironmentVariable("ONE77_TEST_CONNECTION_STRING");

        protected static bool HasDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

        protected static WebinarRepository CreateRepository() =>
            new(new SqlConnectionFactory(ConnectionString!));

        protected static WebinarEventService CreateEventService(WebinarRepository? repository = null) =>
            new(repository ?? CreateRepository());

        protected static WebinarRegistrationService CreateRegistrationService(WebinarRepository? repository = null) =>
            new(repository ?? CreateRepository());

        protected static async Task<IDbConnection> OpenRawAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }

        protected static string Unique(string prefix) =>
            $"{prefix}-{Guid.NewGuid():N}".Substring(0, Math.Min(80, prefix.Length + 33));

        /// <summary>Deletes an event and its registrations directly (children first), bypassing the service layer.</summary>
        protected static async Task DeleteEventCascadeAsync(int eventId)
        {
            using var connection = await OpenRawAsync();
            await connection.ExecuteAsync("DELETE FROM dbo.WebinarRegistrations WHERE EventId = @Id;", new { Id = eventId });
            await connection.ExecuteAsync("DELETE FROM dbo.WebinarEvents WHERE EventId = @Id;", new { Id = eventId });
        }
    }
}
