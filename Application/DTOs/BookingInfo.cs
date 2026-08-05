using Domain.Enums;

namespace Application.DTOs;

public sealed record BookingInfo
{
    public required Guid Id { get; init; }
    public required Guid EventId { get; init; }
    public required BookingStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}
