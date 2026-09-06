namespace Contracts;

public sealed record BookingConfirmed
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public int Seats { get; init; } = 1;
    public DateTime ConfirmedAt { get; init; }
}