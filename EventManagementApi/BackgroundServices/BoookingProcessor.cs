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
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await Task.Delay(2000, stoppingToken);
            var pendingBooking = await bookingService.GetPendingBookingAsync();

            if (pendingBooking != null)
            {
                await bookingService.SetConfirmedStatusAsync(pendingBooking.Id);
            }
        }
    }
}