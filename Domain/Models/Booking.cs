using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Models;

public sealed class Booking
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public Event? Event { get; private set; }

    private Booking()
    {
    }

    private Booking(Guid id, Guid eventId, Guid userId, BookingStatus status, DateTime createdAt)
    {
        Id = id;
        EventId = eventId;
        UserId = userId;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Booking CreatePending(Guid eventId, Guid userId)
    {
        if (eventId == Guid.Empty)
            throw new ValidationException(nameof(EventId), "EventId cannot be empty");

        return new Booking(Guid.NewGuid(), eventId, userId, BookingStatus.Pending, DateTime.UtcNow);
    }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new ValidationException("Booking", "Booking is already cancelled.");

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}