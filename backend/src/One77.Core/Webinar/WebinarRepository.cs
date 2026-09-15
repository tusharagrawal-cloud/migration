using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using One77.Core.Data;

namespace One77.Core.Webinar
{
    /// <summary>
    /// Webinar-specific data access. Deliberately not a generic repository —
    /// every method is a plain, explicit, parameterized query against the
    /// Milestone 2 WebinarEvents/WebinarRegistrations tables. Each method
    /// opens and closes its own connection, matching the pattern already
    /// established by LearnRepository in Milestone 3.
    ///
    /// EventDateTime convention: stored and compared as UTC, matching
    /// CreatedAt/UpdatedAt/RegisteredAt across the whole V1 schema (all
    /// default to SYSUTCDATETIME()). "Has the event passed?" is evaluated
    /// against SYSUTCDATETIME() on the SQL Server side, not the app
    /// server's clock, to avoid clock-skew bugs.
    /// </summary>
    public sealed class WebinarRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public WebinarRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string EventColumns =
            "EventId, Title, EventDateTime, JoinLink, Status, CreatedAt, UpdatedAt";

        private const string RegistrationColumns =
            "RegistrationId, EventId, Name, Email, Phone, RegisteredAt";

        // ---------- Public reads ----------

        public async Task<List<WebinarEvent>> GetUpcomingActiveEventsAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = await connection.QueryAsync<WebinarEvent>(
                    "SELECT " + EventColumns + " FROM dbo.WebinarEvents " +
                    "WHERE Status = 'Active' AND EventDateTime > SYSUTCDATETIME() " +
                    "ORDER BY EventDateTime;");
                return rows.ToList();
            }
        }

        // ---------- Admin event reads/writes ----------

        public async Task<List<WebinarEvent>> GetAllEventsAsync()
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = await connection.QueryAsync<WebinarEvent>(
                    "SELECT " + EventColumns + " FROM dbo.WebinarEvents ORDER BY EventDateTime;");
                return rows.ToList();
            }
        }

        public async Task<WebinarEvent> GetEventByIdAsync(int id)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.QueryFirstOrDefaultAsync<WebinarEvent>(
                    "SELECT " + EventColumns + " FROM dbo.WebinarEvents WHERE EventId = @Id;",
                    new { Id = id });
            }
        }

        public async Task<int> InsertEventAsync(string title, System.DateTime eventDateTime, string joinLink, string status)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.WebinarEvents (Title, EventDateTime, JoinLink, Status)
                      OUTPUT INSERTED.EventId
                      VALUES (@Title, @EventDateTime, @JoinLink, @Status);",
                    new { Title = title, EventDateTime = eventDateTime, JoinLink = joinLink, Status = status });
            }
        }

        public async Task UpdateEventAsync(WebinarEvent webinarEvent)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    @"UPDATE dbo.WebinarEvents
                      SET Title = @Title, EventDateTime = @EventDateTime, JoinLink = @JoinLink,
                          Status = @Status, UpdatedAt = SYSUTCDATETIME()
                      WHERE EventId = @EventId;",
                    webinarEvent);
            }
        }

        // ---------- Registrations ----------

        public async Task<int> InsertRegistrationAsync(int eventId, string name, string email, string phone)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                return await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO dbo.WebinarRegistrations (EventId, Name, Email, Phone)
                      OUTPUT INSERTED.RegistrationId
                      VALUES (@EventId, @Name, @Email, @Phone);",
                    new { EventId = eventId, Name = name, Email = email, Phone = phone });
            }
        }

        public async Task<List<WebinarRegistration>> GetRegistrationsForEventAsync(int eventId)
        {
            using (var connection = await _connectionFactory.OpenConnectionAsync())
            {
                var rows = await connection.QueryAsync<WebinarRegistration>(
                    "SELECT " + RegistrationColumns + " FROM dbo.WebinarRegistrations WHERE EventId = @EventId ORDER BY RegisteredAt;",
                    new { EventId = eventId });
                return rows.ToList();
            }
        }
    }
}
