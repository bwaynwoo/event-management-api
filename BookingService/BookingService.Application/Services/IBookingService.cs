using BookingService.Application.DTOs;

namespace BookingService.Application.Services;

public interface IBookingService
{
    Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
}