using System;
using System.Threading.Tasks;
using One77.Core.Webinar;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Webinar
{
    public class WebinarEventTests : WebinarTestBase
    {
        private readonly ITestOutputHelper _output;
        public WebinarEventTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task Create_Succeeds()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var created = await events.CreateAsync(Unique("Intro Webinar"), DateTime.UtcNow.AddDays(3), "https://meet.example.com/x", null);
            try
            {
                Assert.True(created.EventId > 0);
                Assert.Equal(WebinarEventStatus.Active, created.Status);
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task GetById_RetrievesEvent()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var title = Unique("Retrieve Webinar");
            var created = await events.CreateAsync(title, DateTime.UtcNow.AddDays(2), null, WebinarEventStatus.Active);
            try
            {
                var fetched = await events.GetByIdAsync(created.EventId);
                Assert.Equal(title, fetched.Title);
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task GetById_MissingReturns404()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            await Assert.ThrowsAsync<WebinarNotFoundException>(() => events.GetByIdAsync(-999999));
        }

        [Fact]
        public async Task Update_UpdatesFields()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var created = await events.CreateAsync(Unique("Original Title"), DateTime.UtcNow.AddDays(5), null, WebinarEventStatus.Active);
            try
            {
                var newDateTime = DateTime.UtcNow.AddDays(10);
                var updated = await events.UpdateAsync(created.EventId, "Updated Title", newDateTime, "https://meet.example.com/updated", null);

                Assert.Equal("Updated Title", updated.Title);
                Assert.Equal("https://meet.example.com/updated", updated.JoinLink);
                Assert.True(Math.Abs((updated.EventDateTime - newDateTime).TotalSeconds) < 1);
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task Create_InvalidStatusRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            await Assert.ThrowsAsync<WebinarValidationException>(() =>
                events.CreateAsync(Unique("Bad Status Webinar"), DateTime.UtcNow.AddDays(1), null, "Scheduled"));
        }

        [Fact]
        public async Task Update_InvalidStatusRejected()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var created = await events.CreateAsync(Unique("Status Update Webinar"), DateTime.UtcNow.AddDays(1), null, WebinarEventStatus.Active);
            try
            {
                await Assert.ThrowsAsync<WebinarValidationException>(() =>
                    events.UpdateAsync(created.EventId, null, null, null, "Postponed"));
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task PublicEvents_ActiveUpcoming_Appears()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var created = await events.CreateAsync(Unique("Visible Webinar"), DateTime.UtcNow.AddDays(1), null, WebinarEventStatus.Active);
            try
            {
                var publicList = await events.GetPublicUpcomingEventsAsync();
                Assert.Contains(publicList, e => e.EventId == created.EventId);
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task PublicEvents_Cancelled_DoesNotAppear()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var created = await events.CreateAsync(Unique("Cancelled Webinar"), DateTime.UtcNow.AddDays(1), null, WebinarEventStatus.Cancelled);
            try
            {
                var publicList = await events.GetPublicUpcomingEventsAsync();
                Assert.DoesNotContain(publicList, e => e.EventId == created.EventId);
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task PublicEvents_Completed_DoesNotAppear()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var created = await events.CreateAsync(Unique("Completed Webinar"), DateTime.UtcNow.AddDays(1), null, WebinarEventStatus.Completed);
            try
            {
                var publicList = await events.GetPublicUpcomingEventsAsync();
                Assert.DoesNotContain(publicList, e => e.EventId == created.EventId);
            }
            finally
            {
                await DeleteEventCascadeAsync(created.EventId);
            }
        }

        [Fact]
        public async Task PublicEvents_PastActiveEvent_DoesNotAppear()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var repository = CreateRepository();
            var pastEventId = await repository.InsertEventAsync(Unique("Past Webinar"), DateTime.UtcNow.AddDays(-1), null, WebinarEventStatus.Active);
            try
            {
                var events = CreateEventService(repository);
                var publicList = await events.GetPublicUpcomingEventsAsync();
                Assert.DoesNotContain(publicList, e => e.EventId == pastEventId);
            }
            finally
            {
                await DeleteEventCascadeAsync(pastEventId);
            }
        }

        [Fact]
        public async Task PublicEvents_MultipleUpcoming_ReturnedChronologically()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }

            var events = CreateEventService();
            var later = await events.CreateAsync(Unique("Later Webinar"), DateTime.UtcNow.AddDays(20), null, WebinarEventStatus.Active);
            var sooner = await events.CreateAsync(Unique("Sooner Webinar"), DateTime.UtcNow.AddDays(2), null, WebinarEventStatus.Active);
            try
            {
                var publicList = await events.GetPublicUpcomingEventsAsync();
                var soonerIndex = publicList.FindIndex(e => e.EventId == sooner.EventId);
                var laterIndex = publicList.FindIndex(e => e.EventId == later.EventId);

                Assert.True(soonerIndex >= 0 && laterIndex >= 0);
                Assert.True(soonerIndex < laterIndex, "The nearer event should come first.");
            }
            finally
            {
                await DeleteEventCascadeAsync(later.EventId);
                await DeleteEventCascadeAsync(sooner.EventId);
            }
        }
    }
}
