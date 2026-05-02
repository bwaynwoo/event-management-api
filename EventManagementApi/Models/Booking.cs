using EventManagementApi.Enums;

namespace EventManagementApi.Models;

public class Booking
{
    Guid Id { get; set; }
    Guid EventId { get; set; }
    BookingStatus Status { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime ProcessedAt { get; set; }
}