using EventManagementApi.Exceptions;
using EventManagementApi.Models;
using EventManagementApi.Services;

namespace EventManagementApi.BackgroundServices;

public class BookingProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessor> _logger;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var pendingBookings = await bookingService.GetPendingBookingsAsync();

            if (pendingBookings.Any())
            {
                var tasks = pendingBookings.Select(booking =>
                    ProcessBookingAsync(booking, bookingService, eventService, stoppingToken));
                await Task.WhenAll(tasks);
            }
        }

        _logger.LogInformation("BookingProcessor stopping");
    }

    private async Task ProcessBookingAsync(Booking booking, IBookingService bookingService,
        IEventService eventService, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Processing booking {BookingId}", booking.Id);
        await Task.Delay(2000, stoppingToken);
        await _processingSemaphore.WaitAsync(stoppingToken);
        try
        {
            eventService.GetEvent(booking.EventId);
            booking.Confirm();
            await bookingService.UpdateAsync(booking);
            _logger.LogInformation("Booking {BookingId} confirmed", booking.Id);
        }
        catch (NotFoundException exception)
        {
            booking.Reject();
            await bookingService.UpdateAsync(booking);
            _logger.LogWarning(exception, "Booking {BookingId} rejected", booking.Id);
        }
        catch (OperationCanceledException exception)
        {
            booking.Reject();
            var eventForBooking = eventService.GetEvent(booking.EventId);
            eventForBooking.ReleaseSeats();
            await bookingService.UpdateAsync(booking);
            eventService.UpdateEvent(eventForBooking.Id, eventForBooking);
            _logger.LogWarning(exception, "Booking {BookingId} is canceled", booking.Id);
        }
        catch (NoAvailableSeatsException exception)
        {
            booking.Reject();
            await bookingService.UpdateAsync(booking);
            _logger.LogWarning(exception, "Booking {BookingId} rejected - no available seats for event {EventId}",
                booking.Id, booking.EventId);
        }
        catch (Exception exception)
        {
            booking.Reject();
            var eventForBooking = eventService.GetEvent(booking.EventId);
            eventForBooking.ReleaseSeats();
            await bookingService.UpdateAsync(booking);
            eventService.UpdateEvent(eventForBooking.Id, eventForBooking);
            _logger.LogWarning(exception, "Booking {BookingId} rejected", booking.Id);
        }
        finally
        {
            _processingSemaphore.Release();
        }

        _logger.LogInformation("Booking {BookingId} confirmed", booking.Id);
    }
}