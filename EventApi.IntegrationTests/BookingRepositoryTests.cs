using Domain.Enums;
using Domain.Models;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventApi.IntegrationTests;

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
        var booking = Booking.CreatePending(@event.Id);
        context.Events.Add(@event);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var repository = new BookingRepository(context);

        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(@event.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task AddAsync_SavesBookingToDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 9);
        var booking = Booking.CreatePending(@event.Id);
        context.Events.Add(@event);

        var repository = new BookingRepository(context);

        await repository.AddAsync(booking, CancellationToken.None);
        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(@event.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetPendingBookingIds_ReturnsOnlyPendingBookingIds()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var @event = Event.Create("Test Event", futureDate, futureDate.AddHours(2), 9);
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        var pendingBooking1 = Booking.CreatePending(@event.Id);
        var pendingBooking2 = Booking.CreatePending(@event.Id);
        var confirmedBooking = Booking.CreatePending(@event.Id);
        confirmedBooking.Confirm();
        var rejectedBooking = Booking.CreatePending(@event.Id);
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

        var confirmedBooking = Booking.CreatePending(@event.Id);
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
}