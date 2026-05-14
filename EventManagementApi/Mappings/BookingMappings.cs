using EventManagementApi.DTOs;
using EventManagementApi.Models;

namespace EventManagementApi.Mappings;

public static class BookingMappings
{
    public static BookingResponseDto ToResponse(this Booking booking)
    {
        return new BookingResponseDto(
            booking.Id,
            booking.EventId,
            booking.Status.ToString(),
            booking.CreatedAt,
            booking.ProcessedAt
        );
    }
}