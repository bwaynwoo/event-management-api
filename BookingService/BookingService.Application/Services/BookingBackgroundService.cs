using BookingService.Application.Repositories;
using BookingService.Domain.Enums;
using BookingService.Domain.Models;
using Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Services;

internal sealed class BookingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingBackgroundService> logger,
    IEventPublisher publisher)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing pending bookings");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken stoppingToken)
    {
        List<Booking> pending;
        using (var scope = scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            pending = (await repository.GetPendingAsync(stoppingToken)).ToList();
        }

        foreach (var booking in pending)
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            var current = await repository.GetByIdAsync(booking.Id, stoppingToken);
            if (current is null || current.Status != BookingStatus.Pending)
                continue;

            current.Confirm();
            await repository.UpdateAsync(current, stoppingToken);

            await publisher.PublishBookingConfirmedAsync(new BookingConfirmed
            {
                BookingId = current.Id,
                EventId = current.EventId,
                UserId = current.UserId,
                Seats = 1,
                ConfirmedAt = DateTime.UtcNow
            }, stoppingToken);

            logger.LogInformation("Booking {BookingId} confirmed and published", current.Id);
        }
    }
}