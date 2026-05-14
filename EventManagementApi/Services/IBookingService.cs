using System.Collections.Concurrent;
using EventManagementApi.Models;

namespace EventManagementApi.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId);
    Task<Booking> GetBookingByIdAsync(Guid bookingId);
    Task<Booking?> GetPendingBookingAsync();
    Task SetConfirmedStatusAsync(Guid bookingId);
}