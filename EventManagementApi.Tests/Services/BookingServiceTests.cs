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
        _eventService.Clear();
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
            TotalSeats = 3,
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
            TotalSeats = 3,
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
            TotalSeats = 3,
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
            TotalSeats = 3,
        };
        _eventService.AddEvent(newEvent);
        var booking = await _bookingService.CreateBookingAsync(newEvent.Id);

        booking.Confirm();
        await _bookingService.UpdateAsync(booking);
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

    [Fact(DisplayName = "Создание брони уменьшает AvailableSeats на 1")]
    public async Task CreateBooking_ShouldDecreasesAvailableSeatsByOne()
    {
        var newEvent = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = new DateTime(2025, 1, 1),
            EndAt = new DateTime(2025, 1, 2),
            Id = Guid.NewGuid(),
            TotalSeats = 3,
        };

        _eventService.AddEvent(newEvent);
        await _bookingService.CreateBookingAsync(newEvent.Id);
        var expectedEvent = _eventService.GetEvent(newEvent.Id);

        expectedEvent.AvailableSeats.Should().Be(2);
    }

    [Fact(DisplayName = "Создание нескольких броней (до лимита) — все успешны, у каждой уникальный Id")]
    public async Task CreateBookingsToLimit_ShouldSuccess()
    {
        var newEvent = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = new DateTime(2025, 1, 1),
            EndAt = new DateTime(2025, 1, 2),
            Id = Guid.NewGuid(),
            TotalSeats = 3,
        };

        _eventService.AddEvent(newEvent);
        var firstBooking = await _bookingService.CreateBookingAsync(newEvent.Id);
        var secondBooking = await _bookingService.CreateBookingAsync(newEvent.Id);
        var thirdBooking = await _bookingService.CreateBookingAsync(newEvent.Id);

        firstBooking.Id.Should().NotBe(secondBooking.Id).And.NotBe(thirdBooking.Id);
    }

    [Fact(DisplayName = "После исчерпания мест следующая попытка выбрасывает NoAvailableSeatsException")]
    public async Task WhenNoAvailableSeats_ShouldThrowNoAvailableSeatsException()
    {
        var newEvent = CreateTestEvent(1);
        _eventService.AddEvent(newEvent);
        
        await _bookingService.CreateBookingAsync(newEvent.Id);

        Func<Task> act = async () => await _bookingService.CreateBookingAsync(newEvent.Id);

        await act.Should().ThrowExactlyAsync<NoAvailableSeatsException>();
    }

    [Fact(DisplayName = "После вызова Confirm() бронь возвращает статус Confirmed и заполненный ProcessedAt")]
    public async Task ConfirmBooking_ShouldSucceed()
    {
        var newEvent = CreateTestEvent(1);
        _eventService.AddEvent(newEvent);
        
        var booking = await _bookingService.CreateBookingAsync(newEvent.Id);
        booking.Confirm();

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "После вызова Reject() бронь возвращает статус Rejected и заполненный ProcessedAt")]
    public async Task RejectBooking_ShouldSucceed()
    {
        var newEvent = CreateTestEvent(1);
        _eventService.AddEvent(newEvent);
        
        var booking = await _bookingService.CreateBookingAsync(newEvent.Id);
        booking.Reject();

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "После Reject() и ReleaseSeats() количество свободных мест восстанавливается.")]
    public async Task AfterRejectAndReleaseSeats_ShouldRestoreFreeSeats()
    {
        var newEvent = CreateTestEvent(1);
        _eventService.AddEvent(newEvent);
        
        var booking = await _bookingService.CreateBookingAsync(newEvent.Id);
        booking.Reject();
        newEvent.ReleaseSeats();

        newEvent.AvailableSeats.Should().Be(1);
    }

    [Fact(DisplayName = "После Reject() и ReleaseSeats() можно успешно создать новую бронь на то же место.")]
    public async Task AfterRejectAndReleaseSeats_ShouldAvailableForBooking()
    {
        var newEvent = CreateTestEvent(1);
        _eventService.AddEvent(newEvent);
        
        var rejectedBooking = await _bookingService.CreateBookingAsync(newEvent.Id);
        rejectedBooking.Reject();
        newEvent.ReleaseSeats();
        var newBooking = await _bookingService.CreateBookingAsync(newEvent.Id);

        newBooking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact(DisplayName = "Тест на защиту от овербукинга.")]
    public async Task OverbookingProtectionTest()
    {
        var newEvent = CreateTestEvent(5);
        _eventService.AddEvent(newEvent);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _bookingService.CreateBookingAsync(newEvent.Id))
            .ToList();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            //игнорируем для подсчёта исключений
        }

        int successfulTasks = tasks.Count(t => t.Status == TaskStatus.RanToCompletion);
        int failedTasks =
            tasks.Count(t => t is { Exception.InnerException: NoAvailableSeatsException });

        successfulTasks.Should().Be(5);
        failedTasks.Should().Be(15);
    }

    [Fact(DisplayName = "Тест на уникальность Id при конкурентных запросах")]
    public async Task TestForUniqueIdWithConcurrentQueries()
    {
        var newEvent = CreateTestEvent(10);
        _eventService.AddEvent(newEvent);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _bookingService.CreateBookingAsync(newEvent.Id))
            .ToList();

        var result = await Task.WhenAll(tasks);
        var ids = result.Select(t => t.Id).ToList();

        ids.Should().OnlyHaveUniqueItems();
    }

    private static Event CreateTestEvent(int totalSeats)
    {
        return new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = new DateTime(2025, 1, 1),
            EndAt = new DateTime(2025, 1, 2),
            Id = Guid.NewGuid(),
            TotalSeats = totalSeats,
        };
    }
}