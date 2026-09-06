using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;

namespace BookingService.Domain.Models;

public sealed class Booking
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

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

    public static Booking CreatePending(Guid eventId, Guid userId, TimeProvider? timeProvider = null)
    {
        if (eventId == Guid.Empty)
            throw new ValidationException(nameof(EventId), "EventId cannot be empty");
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;

        return new Booking(Guid.NewGuid(), eventId, userId, BookingStatus.Pending, now);
    }

    public void Confirm(TimeProvider? timeProvider = null)
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Status = BookingStatus.Confirmed;
        ProcessedAt = now;
    }

    public void Reject(TimeProvider? timeProvider = null)
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Status = BookingStatus.Rejected;
        ProcessedAt = now;
    }

    public void Cancel(TimeProvider? timeProvider = null)
    {
        if (Status == BookingStatus.Cancelled)
            throw new ValidationException("Booking", "Booking is already cancelled.");

        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Status = BookingStatus.Cancelled;
        ProcessedAt = now;
    }
}