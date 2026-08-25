using Application.Services;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Models;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace IntegrationTests;

public class BookingRepositoryTests : IAsyncLifetime
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
    public async Task GetByIdAsync_ReturnsBooking()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 9);
        var user = User.Create("testuser", "somepasswordhash");
        var booking = Booking.CreatePending(@event.Id, user.Id);

        context.Events.Add(@event);
        context.Users.Add(user);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var repository = new BookingRepository(context);

        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(@event.Id, result.EventId);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task AddAsync_SavesBookingToDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 9);
        var user = User.Create("testuser", "somepasswordhash");
        var booking = Booking.CreatePending(@event.Id, user.Id);

        context.Events.Add(@event);
        context.Users.Add(user);

        var repository = new BookingRepository(context);

        await repository.AddAsync(booking, CancellationToken.None);
        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(@event.Id, result.EventId);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetPendingBookingIds_ReturnsOnlyPendingBookingIds()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 9);
        var user = User.Create("testuser", "somepasswordhash");
        context.Users.Add(user);
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        var pendingBooking1 = Booking.CreatePending(@event.Id, user.Id);
        var pendingBooking2 = Booking.CreatePending(@event.Id, user.Id);
        var confirmedBooking = Booking.CreatePending(@event.Id, user.Id);
        confirmedBooking.Confirm();
        var rejectedBooking = Booking.CreatePending(@event.Id, user.Id);
        rejectedBooking.Reject();

        context.Bookings.AddRange(pendingBooking1, pendingBooking2, confirmedBooking, rejectedBooking);
        await context.SaveChangesAsync();

        var repository = new BookingRepository(context);
        var result = await repository.GetPendingBookingIds(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(pendingBooking1.Id, result);
        Assert.Contains(pendingBooking2.Id, result);
        Assert.DoesNotContain(confirmedBooking.Id, result);
        Assert.DoesNotContain(rejectedBooking.Id, result);
    }

    [Fact]
    public async Task GetPendingBookingIds_NoPendingBookings_ReturnsEmptyList()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 9);
        context.Events.Add(@event);
        var user = User.Create("testuser", "somepasswordhash");

        var confirmedBooking = Booking.CreatePending(@event.Id, user.Id);
        context.Users.Add(user);
        confirmedBooking.Confirm();
        context.Bookings.Add(confirmedBooking);
        await context.SaveChangesAsync();

        var repository = new BookingRepository(context);
        var result = await repository.GetPendingBookingIds(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendingBookingIds_NoBookings_ReturnsEmptyList()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var repository = new BookingRepository(context);
        var result = await repository.GetPendingBookingIds(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateBookingAsync_PastEvent_ThrowsEventAlreadyStartedException()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var pastDate = DateTime.UtcNow.AddHours(-1);

        var @event = (Event)Activator.CreateInstance(typeof(Event), nonPublic: true)!;

        typeof(Event).GetProperty("Id")!.SetValue(@event, Guid.NewGuid());
        typeof(Event).GetProperty("Title")!.SetValue(@event, "Past Event");
        typeof(Event).GetProperty("StartAt")!.SetValue(@event, pastDate);
        typeof(Event).GetProperty("EndAt")!.SetValue(@event, pastDate.AddHours(2));
        typeof(Event).GetProperty("TotalSeats")!.SetValue(@event, 10);
        typeof(Event).GetProperty("AvailableSeats")!.SetValue(@event, 10);

        var user = User.Create("testuser", "testhash");

        context.Events.Add(@event);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var bookingRepository = new BookingRepository(context);
        var eventRepository = new EventRepository(context);
        var bookingLock = new SemaphoreSlim(1, 1);
        var bookingService = new BookingService(bookingRepository, eventRepository, bookingLock, TimeProvider.System);

        await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
            bookingService.CreateBookingAsync(@event.Id, user.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenUserHasMaxActiveBookings_ThrowsBookingLimitExceededException()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 100);

        var user = User.Create("testuser", "testhash");

        context.Events.Add(@event);
        context.Users.Add(user);

        for (int i = 0; i < 10; i++)
        {
            var activeBooking = Booking.CreatePending(@event.Id, user.Id);
            context.Bookings.Add(activeBooking);
            @event.TryReserveSeats();
        }

        await context.SaveChangesAsync();

        var bookingRepository = new BookingRepository(context);
        var eventRepository = new EventRepository(context);
        var bookingLock = new SemaphoreSlim(1, 1);
        var bookingService = new BookingService(bookingRepository, eventRepository, bookingLock, TimeProvider.System);

        await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            bookingService.CreateBookingAsync(@event.Id, user.Id, CancellationToken.None));

        var bookingCount = await context.Bookings.CountAsync(b => b.UserId == user.Id);
        Assert.Equal(10, bookingCount);
    }

    [Fact]
    public async Task CreateBookingAsync_LimitsArePerUser_DoesNotAffectOtherUsers()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 100);

        var user1 = User.Create("user1", "hash1");
        var user2 = User.Create("user2", "hash2");

        context.Events.Add(@event);
        context.Users.AddRange(user1, user2);

        for (int i = 0; i < 10; i++)
        {
            var booking = Booking.CreatePending(@event.Id, user1.Id);
            context.Bookings.Add(booking);
            @event.TryReserveSeats();
        }

        await context.SaveChangesAsync();

        var bookingRepository = new BookingRepository(context);
        var eventRepository = new EventRepository(context);
        var bookingLock = new SemaphoreSlim(1, 1);
        var bookingService = new BookingService(bookingRepository, eventRepository, bookingLock, TimeProvider.System);

        var result = await bookingService.CreateBookingAsync(@event.Id, user2.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(user2.Id, result.UserId);
        Assert.Equal(BookingStatus.Pending, result.Status);

        await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            bookingService.CreateBookingAsync(@event.Id, user1.Id, CancellationToken.None));

        var user2Bookings = await context.Bookings.CountAsync(b => b.UserId == user2.Id);
        Assert.Equal(1, user2Bookings);
    }
}