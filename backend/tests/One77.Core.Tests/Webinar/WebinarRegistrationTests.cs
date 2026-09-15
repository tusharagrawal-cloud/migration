using System;
using System.Linq;
using System.Threading.Tasks;
using One77.Core.Webinar;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Webinar
{
    public class WebinarRegistrationTests : WebinarTestBase
    {
        private readonly ITestOutputHelper _output;
        public WebinarRegistrationTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Register_AgainstValidActiveUpcomingEvent_Succeeds()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var registrations = CreateRegistrationService();
            var created = await events.CreateAsync(Unique("Register Webinar"), DateTime.UtcNow.AddDays(3), null, WebinarEventStatus.Active);
            try
            {
                var result = await registrations.RegisterAsync(created.EventId, "Jane Shooter", "jane@example.com", "+61400000000");
                Assert.True(result.RegistrationId > 0);
                Assert.Equal(created.EventId, result.EventId);
                Assert.Equal(created.Title, result.EventTitle);
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task Register_StoresRegistrationCorrectly()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var registrations = CreateRegistrationService();
            var created = await events.CreateAsync(Unique("Storage Check Webinar"), DateTime.UtcNow.AddDays(3), null, WebinarEventStatus.Active);
            try
            {
                await registrations.RegisterAsync(created.EventId, "John Smith", "john@example.com", "+61411111111");

                var list = await registrations.GetRegistrationsForEventAsync(created.EventId);
                var stored = Assert.Single(list);
                Assert.Equal("John Smith", stored.Name);
                Assert.Equal("john@example.com", stored.Email);
                Assert.Equal("+61411111111", stored.Phone);
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task Register_AgainstNonexistentEvent_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var registrations = CreateRegistrationService();
            await Assert.ThrowsAsync<WebinarValidationException>(() =>
                registrations.RegisterAsync(-999999, "Ghost", "ghost@example.com", "+61400000001"));
        }

        [Fact]
        public async Task Register_AgainstCancelledEvent_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var registrations = CreateRegistrationService();
            var created = await events.CreateAsync(Unique("Cancelled Register Webinar"), DateTime.UtcNow.AddDays(3), null, WebinarEventStatus.Cancelled);
            try
            {
                await Assert.ThrowsAsync<WebinarValidationException>(() =>
                    registrations.RegisterAsync(created.EventId, "A", "a@example.com", "+61400000002"));
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task Register_AgainstCompletedEvent_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var registrations = CreateRegistrationService();
            var created = await events.CreateAsync(Unique("Completed Register Webinar"), DateTime.UtcNow.AddDays(3), null, WebinarEventStatus.Completed);
            try
            {
                await Assert.ThrowsAsync<WebinarValidationException>(() =>
                    registrations.RegisterAsync(created.EventId, "B", "b@example.com", "+61400000003"));
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task Register_AgainstPastEvent_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var repository = CreateRepository();
            var pastEventId = await repository.InsertEventAsync(Unique("Past Register Webinar"), DateTime.UtcNow.AddDays(-2), null, WebinarEventStatus.Active);
            try
            {
                var registrations = CreateRegistrationService(repository);
                await Assert.ThrowsAsync<WebinarValidationException>(() =>
                    registrations.RegisterAsync(pastEventId, "C", "c@example.com", "+61400000004"));
            }
            finally
            {
                await DeleteEventCascadeAsync(pastEventId);
            }
        }

        [Fact]
        public async Task Register_MissingName_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var registrations = CreateRegistrationService();
            var created = await events.CreateAsync(Unique("Missing Name Webinar"), DateTime.UtcNow.AddDays(3), null, WebinarEventStatus.Active);
            try
            {
                await Assert.ThrowsAsync<WebinarValidationException>(() =>
                    registrations.RegisterAsync(created.EventId, "", "d@example.com", "+61400000005"));
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task Register_MissingEmail_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var registrations = CreateRegistrationService();
            var created = await events.CreateAsync(Unique("Missing Email Webinar"), DateTime.UtcNow.AddDays(3), null, WebinarEventStatus.Active);
            try
            {
                await Assert.ThrowsAsync<WebinarValidationException>(() =>
                    registrations.RegisterAsync(created.EventId, "E", "", "+61400000006"));
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task Register_MissingPhone_Rejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var registrations = CreateRegistrationService();
            var created = await events.CreateAsync(Unique("Missing Phone Webinar"), DateTime.UtcNow.AddDays(3), null, WebinarEventStatus.Active);
            try
            {
                await Assert.ThrowsAsync<WebinarValidationException>(() =>
                    registrations.RegisterAsync(created.EventId, "F", "f@example.com", ""));
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task AdminRegistrationList_ReturnsRegistrationsForCorrectEvent()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var registrations = CreateRegistrationService();
            var eventA = await events.CreateAsync(Unique("Event A"), DateTime.UtcNow.AddDays(3), null, WebinarEventStatus.Active);
            var eventB = await events.CreateAsync(Unique("Event B"), DateTime.UtcNow.AddDays(4), null, WebinarEventStatus.Active);
            await registrations.RegisterAsync(eventA.EventId, "In A", "ina@example.com", "+61400000007");
            await registrations.RegisterAsync(eventB.EventId, "In B", "inb@example.com", "+61400000008");
            try
            {
                var listA = await registrations.GetRegistrationsForEventAsync(eventA.EventId);
                Assert.Single(listA);
                Assert.Equal("In A", listA.Single().Name);
            }
            finally
            {
                await DeleteEventCascadeAsync(eventA.EventId);
                await DeleteEventCascadeAsync(eventB.EventId);
            }
        }

        [Fact]
        public async Task AdminRegistrationList_MissingEventReturns404()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var registrations = CreateRegistrationService();
            await Assert.ThrowsAsync<WebinarNotFoundException>(() => registrations.GetRegistrationsForEventAsync(-999999));
        }
    }
}
