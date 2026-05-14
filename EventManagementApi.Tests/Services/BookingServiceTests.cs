using EventManagementApi.Enums;
using EventManagementApi.Exceptions;
using EventManagementApi.Models;
using EventManagementApi.Services;
using FluentAssertions;

namespace EventManagementApi.Tests.Services;

public class BookingServiceTests
{
    private readonly BookingService _bookingService;
    private readonly EventService _eventService;

    public BookingServiceTests()
    {
        _eventService = new EventService();
        _bookingService = new BookingService(_eventService);
    }

    [Fact(DisplayName = "Создание брони для существующего события")]
    public async Task CreateBooking_ShouldSuccess()
    {
        var newEvent = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = new DateTime(2025, 1, 1),
            EndAt = new DateTime(2025, 1, 2),
            Id = Guid.NewGuid(),
        };

        _eventService.AddEvent(newEvent);
        var booking = await _bookingService.CreateBookingAsync(newEvent.Id);

        booking.EventId.Should().Be(newEvent.Id);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.Id.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "Создание нескольких броней для одного события — все создаются с уникальными Id")]
    public async Task CreateBookingsForOneEvent_ShouldSuccess_WithDifferentIds()
    {
        var newEvent = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = new DateTime(2025, 1, 1),
            EndAt = new DateTime(2025, 1, 2),
            Id = Guid.NewGuid(),
        };
        _eventService.AddEvent(newEvent);

        var bookings = new List<Booking>
        {
            await _bookingService.CreateBookingAsync(newEvent.Id),
            await _bookingService.CreateBookingAsync(newEvent.Id),
            await _bookingService.CreateBookingAsync(newEvent.Id),
        };

        bookings.Select(e => e.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact(DisplayName = "Получение брони по Id — возвращается корректная информация")]
    public async Task GetBookingsById_ShouldReturnCorrectInfo()
    {
        var newEvent = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = new DateTime(2025, 1, 1),
            EndAt = new DateTime(2025, 1, 2),
            Id = Guid.NewGuid(),
        };
        _eventService.AddEvent(newEvent);
        var booking = await _bookingService.CreateBookingAsync(newEvent.Id);

        var expectedBooking = await _bookingService.GetBookingByIdAsync(booking.Id);

        expectedBooking.EventId.Should().Be(booking.EventId);
        expectedBooking.Status.Should().Be(booking.Status);
        expectedBooking.Id.Should().Be(booking.Id);
    }

    [Fact(DisplayName = "Получение брони отражает изменение статуса (после Confirm/Reject)")]
    public async Task Booking_ShouldChangedStatus()
    {
        var newEvent = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = new DateTime(2025, 1, 1),
            EndAt = new DateTime(2025, 1, 2),
            Id = Guid.NewGuid(),
        };
        _eventService.AddEvent(newEvent);
        var booking = await _bookingService.CreateBookingAsync(newEvent.Id);

        await _bookingService.SetConfirmedStatusAsync(booking.Id);
        var expectedBooking = await _bookingService.GetBookingByIdAsync(booking.Id);

        expectedBooking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact(DisplayName = "Создание брони для несуществующего события")]
    public async Task CreateBooking_ShouldFail_WhenEventDoesNotExist()
    {
        Func<Task> act = async () => await _bookingService.CreateBookingAsync(Guid.NewGuid());
        
        await act.Should().ThrowExactlyAsync<NotFoundException>();
    }
    
    [Fact(DisplayName = "Создание брони для удалённого события")]
    public async Task CreateBooking_ShouldFail_WhenEventDeleted()
    {
        var newEvent = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = new DateTime(2025, 1, 1),
            EndAt = new DateTime(2025, 1, 2),
            Id = Guid.NewGuid(),
        };

        _eventService.AddEvent(newEvent);
        _eventService.RemoveEvent(newEvent.Id);
        Func<Task> act = async () => await _bookingService.CreateBookingAsync(Guid.NewGuid());
        
        await act.Should().ThrowExactlyAsync<NotFoundException>();
    }
    
    [Fact(DisplayName = "Получение брони по несуществующему Id")]
    public async Task GetBooking_ShouldFail_WhenEventDoesNotExist()
    {
        Func<Task> act = async () => await _bookingService.GetBookingByIdAsync(Guid.NewGuid());
        
        await act.Should().ThrowExactlyAsync<NotFoundException>();
    }
}