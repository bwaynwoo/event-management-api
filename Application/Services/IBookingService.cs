using Application.DTOs;

namespace Application.Services;

public interface IBookingService
{
    Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}