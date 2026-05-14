using EventManagementApi.Services;

namespace EventManagementApi.BackgroundServices;

public class BookingProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessor> _logger;

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

            var pendingBooking = await bookingService.GetPendingBookingAsync();

            if (pendingBooking != null)
            {
                _logger.LogInformation("Processing booking {BookingId}", pendingBooking.Id);
                await Task.Delay(2000, stoppingToken);
                await bookingService.SetConfirmedStatusAsync(pendingBooking.Id);
                _logger.LogInformation("Booking {BookingId} confirmed", pendingBooking.Id);
            }
        }

        _logger.LogInformation("BookingProcessor stopping");
    }
}