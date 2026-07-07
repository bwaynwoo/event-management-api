using EventManagementApi.DTOs;

namespace EventManagementApi.Services;

internal interface IBookingService
{
    Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}