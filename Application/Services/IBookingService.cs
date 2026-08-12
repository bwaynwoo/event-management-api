using Application.DTOs;
using Domain.Enums;

namespace Application.Services;

public interface IBookingService
{
    Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task CancelBookingAsync(Guid bookingId, Guid userId, Role userRole, CancellationToken cancellationToken = default);
}