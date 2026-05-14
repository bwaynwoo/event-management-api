using System.Collections.Concurrent;
using EventManagementApi.Enums;
using EventManagementApi.Exceptions;
using EventManagementApi.Models;

namespace EventManagementApi.Services;

public class BookingService(IEventService eventService) : IBookingService
{
    private static readonly ConcurrentDictionary<Guid, Booking> Bookings = new();

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        eventService.GetEvent(eventId);

        var booking = new Booking
        {
            EventId = eventId,
        };
        Bookings.TryAdd(booking.Id, booking);
        return await Task.FromResult(booking);
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        if (!Bookings.TryGetValue(bookingId, out var bookingItem))
        {
            throw new NotFoundException("Booking", bookingId);
        }

        return await Task.FromResult(bookingItem);
    }

    public async Task<Booking?> GetPendingBookingAsync()
    {
        var pendingBookings =
            Bookings.Where(e => e.Value.Status == BookingStatus.Pending);
        if (!pendingBookings.Any())
        {
            return null;
        }

        var pendingBooking = pendingBookings.FirstOrDefault().Value;

        return await Task.FromResult(pendingBooking);
    }

    public Task SetConfirmedStatusAsync(Guid bookingId)
    {
        Bookings[bookingId].Status = BookingStatus.Confirmed;
        Bookings[bookingId].ProcessedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}