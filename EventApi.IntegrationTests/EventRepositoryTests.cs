using EventApi.DataAccess;
using EventApi.Exceptions;
using EventApi.Models;
using EventApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventApi.IntegrationTests;

public class EventRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.Migrate();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        context.Bookings.RemoveRange(context.Bookings);
        context.Events.RemoveRange(context.Events);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEvent_ReturnsEvent()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 100, "Description", "Location");
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        var repository = new EventRepository(context);
        var result = await repository.GetByIdAsync(@event.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(@event.Id, result.Id);
        Assert.Equal("Test Event", result.Title);
        Assert.Equal(100, result.TotalSeats);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var nonExistingId = Guid.NewGuid();

        var repository = new EventRepository(context);
        await Assert.ThrowsAsync<NotFoundException>(() => repository.GetByIdAsync(nonExistingId, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetByIdAsync_MultipleEvents_ReturnsCorrectEvent()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var event1 = Event.Create("Event 1", futureDate, futureDate.AddHours(2), 50);
        var event2 = Event.Create("Event 2", futureDate.AddDays(1), futureDate.AddDays(1).AddHours(3), 100);
        context.Events.AddRange(event1, event2);
        await context.SaveChangesAsync();

        var repository = new EventRepository(context);
        var result = await repository.GetByIdAsync(event2.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(event2.Id, result.Id);
        Assert.Equal("Event 2", result.Title);
        Assert.NotEqual(event1.Id, result.Id);
    }

    [Fact]
    public async Task AddAsync_SavesEventToDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("New Event", futureDate, futureDate.AddHours(2), 100);

        var repository = new EventRepository(context);
        await repository.AddAsync(@event, CancellationToken.None);
        await context.SaveChangesAsync();

        await using var assertContext = CreateContext();
        var savedEvent = await assertContext.Events.FirstOrDefaultAsync(e => e.Id == @event.Id);

        Assert.NotNull(savedEvent);
        Assert.Equal(@event.Id, savedEvent.Id);
        Assert.Equal("New Event", savedEvent.Title);
        Assert.Equal(100, savedEvent.TotalSeats);
    }

    [Fact]
    public async Task AddAsync_MultipleEvents_AllSavedToDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var event1 = Event.Create("Event 1", futureDate, futureDate.AddHours(2), 50);
        var event2 = Event.Create("Event 2", futureDate.AddDays(2), futureDate.AddDays(2).AddHours(3), 100);

        var repository = new EventRepository(context);
        await repository.AddAsync(event1, CancellationToken.None);
        await repository.AddAsync(event2, CancellationToken.None);
        await context.SaveChangesAsync();

        var allEvents = await context.Events.ToListAsync();
        Assert.Equal(2, allEvents.Count);
        Assert.Contains(allEvents, e => e.Id == event1.Id);
        Assert.Contains(allEvents, e => e.Id == event2.Id);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsChangesToDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 100);

        var repository = new EventRepository(context);
        await repository.AddAsync(@event, CancellationToken.None);

        await repository.SaveChangesAsync(CancellationToken.None);

        await using var assertContext = CreateContext();
        var savedEvent = await assertContext.Events.FirstOrDefaultAsync(e => e.Id == @event.Id);

        Assert.NotNull(savedEvent);
        Assert.Equal(@event.Id, savedEvent.Id);
        Assert.Equal("Test Event", savedEvent.Title);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutChanges_CompletesSuccessfully()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var exception = await Record.ExceptionAsync(() => repository.SaveChangesAsync(CancellationToken.None)
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task SaveChangesAsync_AfterMultipleAdds_PersistsAllChanges()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var event1 = Event.Create("Event 1", futureDate, futureDate.AddHours(2), 50);
        var event2 = Event.Create("Event 2", futureDate.AddDays(2), futureDate.AddDays(2).AddHours(3), 100);

        var repository = new EventRepository(context);
        await repository.AddAsync(event1, CancellationToken.None);
        await repository.AddAsync(event2, CancellationToken.None);

        await repository.SaveChangesAsync(CancellationToken.None);

        var allEvents = await context.Events.ToListAsync();
        Assert.Equal(2, allEvents.Count);
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsPaginatedEvents()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);

        for (int i = 1; i <= 10; i++)
        {
            var @event = Event.Create($"Event {i}", futureDate.AddDays(i), futureDate.AddDays(i).AddHours(2), 100);
            context.Events.Add(@event);
        }

        await context.SaveChangesAsync();

        var repository = new EventRepository(context);
        var (items, totalCount) = await repository.GetEventsAsync(1, 5, null, null, null, CancellationToken.None);

        Assert.Equal(5, items.Count);
        Assert.Equal(10, totalCount);
    }

    [Fact]
    public async Task GetEventsAsync_FilterByDateRange_ReturnsFilteredEvents()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var baseDate = DateTime.UtcNow.AddDays(1);

        var event1 = Event.Create("Early Event", baseDate, baseDate.AddHours(2), 50);
        var event2 = Event.Create("Middle Event", baseDate.AddDays(2), baseDate.AddDays(2).AddHours(2), 75);
        var event3 = Event.Create("Late Event", baseDate.AddDays(5), baseDate.AddDays(5).AddHours(2), 100);

        context.Events.AddRange(event1, event2, event3);
        await context.SaveChangesAsync();

        var repository = new EventRepository(context);
        var from = baseDate.AddDays(1);
        var to = baseDate.AddDays(3);
        var (items, totalCount) = await repository.GetEventsAsync(1, 10, from, to, null, CancellationToken.None);

        Assert.Single(items);
        Assert.Equal(1, totalCount);
        Assert.Equal("Middle Event", items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_FilterByTitle_ReturnsMatchingEvents()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);

        var event1 = Event.Create("Conference Tech", futureDate, futureDate.AddHours(2), 100);
        var event2 = Event.Create("Tech Workshop", futureDate.AddDays(1), futureDate.AddDays(1).AddHours(3), 50);
        var event3 = Event.Create("Music Festival", futureDate.AddDays(2), futureDate.AddDays(2).AddHours(4), 200);

        context.Events.AddRange(event1, event2, event3);
        await context.SaveChangesAsync();

        var repository = new EventRepository(context);
        var (items, totalCount) = await repository.GetEventsAsync(1, 10, null, null, "tech", CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal(2, totalCount);
        Assert.Contains(items, e => e.Title == "Conference Tech");
        Assert.Contains(items, e => e.Title == "Tech Workshop");
    }

    [Fact]
    public async Task GetEventsAsync_NoEvents_ReturnsEmptyList()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var repository = new EventRepository(context);
        var (items, totalCount) = await repository.GetEventsAsync(1, 10, null, null, null, CancellationToken.None);

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEvent_RemovesFromDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Event to Delete", futureDate, futureDate.AddHours(2), 100);
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        var repository = new EventRepository(context);
        await repository.DeleteAsync(@event, CancellationToken.None);

        await using var assertContext = CreateContext();
        var deletedEvent = await assertContext.Events.FirstOrDefaultAsync(e => e.Id == @event.Id);
        Assert.Null(deletedEvent);
    }

    [Fact]
    public async Task DeleteAsync_WithBookings_CascadesOrRestricts()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Event with Bookings", futureDate, futureDate.AddHours(2), 100);
        context.Events.Add(@event);

        var booking = Booking.CreatePending(@event.Id);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var repository = new EventRepository(context);

        await repository.DeleteAsync(@event, CancellationToken.None);

        await using var assertContext = CreateContext();
        var deletedEvent = await assertContext.Events.FirstOrDefaultAsync(e => e.Id == @event.Id);
        Assert.Null(deletedEvent);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEvent_ThrowsException()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Ghost Event", futureDate, futureDate.AddHours(2), 100);

        context.Events.Attach(@event);

        var repository = new EventRepository(context);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            repository.DeleteAsync(@event, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DeleteAsync_MultipleDeletes_AllRemoved()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var event1 = Event.Create("Event 1", futureDate, futureDate.AddHours(2), 50);
        var event2 = Event.Create("Event 2", futureDate.AddDays(1), futureDate.AddDays(1).AddHours(2), 75);

        context.Events.AddRange(event1, event2);
        await context.SaveChangesAsync();

        var repository = new EventRepository(context);
        await repository.DeleteAsync(event1, CancellationToken.None);
        await repository.DeleteAsync(event2, CancellationToken.None);

        var remainingEvents = await context.Events.ToListAsync();
        Assert.Empty(remainingEvents);
    }
}