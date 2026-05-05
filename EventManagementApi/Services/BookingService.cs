using System.Collections.Concurrent;
using EventManagementApi.Exceptions;
using EventManagementApi.Models;

namespace EventManagementApi.Services;

public class BookingService(IEventService eventService) : IBookingService
{
    private static readonly ConcurrentDictionary<Guid, Booking> Bookings = new();

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        try
        {
            var existingEvent = eventService.GetEvent(eventId);
        }
        catch (NotFoundException e)
        {
            return null;
        }

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
            return null;
        }

        return await Task.FromResult(bookingItem);
    }
}