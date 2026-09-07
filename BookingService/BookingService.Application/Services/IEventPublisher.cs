using Contracts;

namespace BookingService.Application.Services;

public interface IEventPublisher
{
    Task PublishBookingConfirmedAsync(BookingConfirmed message, CancellationToken cancellationToken = default);
}
