using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Schema
{
    public class WebinarSchemaTests : SchemaTestBase
    {
        private readonly ITestOutputHelper _output;
        public WebinarSchemaTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task CanInsert_EventAndRegistration()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var title = Unique("Intro to PCP Airguns");
            try
            {
                var eventId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.WebinarEvents (Title, EventDateTime) OUTPUT INSERTED.EventId VALUES (@Title, @When);",
                    new { Title = title, When = DateTime.UtcNow.AddDays(7) });

                var registrationId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO dbo.WebinarRegistrations (EventId, Name, Email, Phone) OUTPUT INSERTED.RegistrationId VALUES (@EventId, @Name, @Email, @Phone);",
                    new { EventId = eventId, Name = "Jane Shooter", Email = "jane@example.com", Phone = "+61400000000" });

                Assert.True(eventId > 0);
                Assert.True(registrationId > 0);
            }
            finally
            {
                await connection.ExecuteAsync(
                    "DELETE FROM dbo.WebinarRegistrations WHERE EventId IN (SELECT EventId FROM dbo.WebinarEvents WHERE Title = @Title);",
                    new { Title = title });
                await connection.ExecuteAsync("DELETE FROM dbo.WebinarEvents WHERE Title = @Title;", new { Title = title });
            }
        }

        [Fact]
        public async Task RegistrationAgainstMissingEvent_IsRejectedByForeignKey()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.WebinarRegistrations (EventId, Name, Email, Phone) VALUES (-999999, 'Ghost', 'ghost@example.com', '+61400000001');"));

            Assert.Contains("FK_WebinarRegistrations_Event", ex.Message);
        }

        [Fact]
        public async Task InvalidEventStatus_IsRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            using var connection = await OpenAsync();
            var title = Unique("Bad Status Event");

            var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.WebinarEvents (Title, EventDateTime, Status) VALUES (@Title, @When, 'Postponed');",
                    new { Title = title, When = DateTime.UtcNow.AddDays(7) }));

            Assert.Contains("CK_WebinarEvents_Status", ex.Message);
        }
    }
}
