using Application.DTOs;
using Application.Repositories;
using Application.Services;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Models;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Services;

public sealed class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;
    private readonly SemaphoreSlim _bookingLock = new(1, 1);

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddSingleton(_bookingLock);
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
        _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private async Task<Guid> CreateTestEventAsync(int totalSeats = 10)
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var created = await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Test Event",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = totalSeats
        });
        return created.Id;
    }

    #region CreateBookingAsync Tests

    [Fact]
    public async Task CreateBookingAsync_WithValidEventId_ReturnsBookingInfoWithPendingStatus()
    {
        var eventId = await CreateTestEventAsync();
        var userId = Guid.NewGuid();
        var result = await _bookingService.CreateBookingAsync(eventId, userId);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Null(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_WithValidEventId_SetsCreatedAt()
    {
        var eventId = await CreateTestEventAsync();
        var before = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        var result = await _bookingService.CreateBookingAsync(eventId, userId);

        var after = DateTime.UtcNow;
        Assert.InRange(result.CreatedAt, before, after);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNonExistentEvent_ThrowsNotFoundException()
    {
        var invalidEventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var exception =
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _bookingService.CreateBookingAsync(invalidEventId, userId));
        Assert.Equal("Event not found", exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_AllCreatedWithUniqueIds()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 5);
        var userId = Guid.NewGuid();

        var results = new List<BookingInfo>();
        for (int i = 0; i < 5; i++)
            results.Add(await _bookingService.CreateBookingAsync(eventId, userId));

        var uniqueIds = results.Select(r => r.Id).Distinct();
        Assert.Equal(5, uniqueIds.Count());
    }

    [Fact]
    public async Task CreateBookingAsync_WhenNoSeatsAvailable_ThrowsNoAvailableSeatsException()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 1);
        var userId = Guid.NewGuid();
        await _bookingService.CreateBookingAsync(eventId, userId);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(eventId, userId));
    }

    [Fact]
    public async Task CreateBookingAsync_DecrementsAvailableSeats()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 3);
        var userId = Guid.NewGuid();

        await _bookingService.CreateBookingAsync(eventId, userId);
        await _bookingService.CreateBookingAsync(eventId, userId);

        var eventInfo = await _eventService.GetEventByIdAsync(eventId);
        Assert.Equal(1, eventInfo.AvailableSeats);
    }

    #endregion

    #region GetBookingByIdAsync Tests

    [Fact]
    public async Task GetBookingByIdAsync_WithValidId_ReturnsCorrectBookingInfo()
    {
        var eventId = await CreateTestEventAsync();
        var userId = Guid.NewGuid();
        var created = await _bookingService.CreateBookingAsync(eventId, userId);

        var result = await _bookingService.GetBookingByIdAsync(created.Id, userId, Role.User);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        var invalidId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(invalidId, Guid.Empty, Role.User));
        Assert.Equal("Booking not found", exception.Message);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_DoesNotOverbookEvent()
    {
        const int totalSeats = 5;
        const int concurrentRequests = 20;
        var eventId = await CreateTestEventAsync(totalSeats: totalSeats);
        var userId = Guid.NewGuid();

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    await bookingService.CreateBookingAsync(eventId, userId);
                    return true;
                }
                catch (NoAvailableSeatsException)
                {
                    return false;
                }
            }));

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r);
        Assert.Equal(totalSeats, successCount);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllSuccessfulBookingsHaveUniqueIds()
    {
        const int totalSeats = 10;
        const int concurrentRequests = 10;
        var eventId = await CreateTestEventAsync(totalSeats: totalSeats);
        var bookingIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();
        var userId = Guid.NewGuid();

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var booking = await bookingService.CreateBookingAsync(eventId, userId);
                bookingIds.Add(booking.Id);
            }));

        await Task.WhenAll(tasks);

        Assert.Equal(totalSeats, bookingIds.Distinct().Count());
    }

    #endregion

    #region CancelBookingAsync Tests

    [Fact]
    public async Task CancelBookingAsync_WithValidData_CancelsBooking()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 10);
        var userId = Guid.NewGuid();
        var booking = await _bookingService.CreateBookingAsync(eventId, userId);

        await _bookingService.CancelBookingAsync(booking.Id, userId, Role.User);

        var cancelledBooking = await _bookingService.GetBookingByIdAsync(booking.Id, userId, Role.User);
        Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_WithAdminRole_CanCancelAnyBooking()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 10);
        var userId = Guid.NewGuid();
        var booking = await _bookingService.CreateBookingAsync(eventId, userId);

        await _bookingService.CancelBookingAsync(booking.Id, Guid.NewGuid(), Role.Admin);

        var cancelledBooking = await _bookingService.GetBookingByIdAsync(booking.Id, userId, Role.Admin);
        Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_WithWrongUser_ThrowsForbiddenException()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 10);
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var booking = await _bookingService.CreateBookingAsync(eventId, userId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _bookingService.CancelBookingAsync(booking.Id, anotherUserId, Role.User));
    }

    [Fact]
    public async Task CancelBookingAsync_WithNonExistentBooking_ThrowsNotFoundException()
    {
        var invalidBookingId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _bookingService.CancelBookingAsync(invalidBookingId, Guid.NewGuid(), Role.User));
    }

    [Fact]
    public async Task CancelBookingAsync_AfterEventStarted_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var @event = Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 10);
        var booking = Booking.CreatePending(eventId, userId);

        var bookingRepoMock = new Mock<IBookingRepository>();
        var eventRepoMock = new Mock<IEventRepository>();
        var timeProviderMock = new Mock<TimeProvider>();

        bookingRepoMock
            .Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        eventRepoMock
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        timeProviderMock
            .Setup(x => x.GetUtcNow())
            .Returns(new DateTimeOffset(DateTime.UtcNow.AddDays(2)));

        var bookingService = new BookingService(
            bookingRepoMock.Object,
            eventRepoMock.Object,
            new SemaphoreSlim(1, 1),
            timeProviderMock.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            bookingService.CancelBookingAsync(bookingId, userId, Role.User));
    }

    [Fact]
    public async Task CancelBookingAsync_AlreadyCancelled_ThrowsValidationException()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 10);
        var userId = Guid.NewGuid();
        var booking = await _bookingService.CreateBookingAsync(eventId, userId);

        await _bookingService.CancelBookingAsync(booking.Id, userId, Role.User);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _bookingService.CancelBookingAsync(booking.Id, userId, Role.User));
    }

    [Fact]
    public async Task CancelBookingAsync_ReleasesSeat()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 10);
        var userId = Guid.NewGuid();
        var booking = await _bookingService.CreateBookingAsync(eventId, userId);

        var eventInfoBefore = await _eventService.GetEventByIdAsync(eventId);

        await _bookingService.CancelBookingAsync(booking.Id, userId, Role.User);

        var eventInfoAfter = await _eventService.GetEventByIdAsync(eventId);
        Assert.Equal(eventInfoBefore.AvailableSeats + 1, eventInfoAfter.AvailableSeats);
    }

    #endregion
}