using EventManagementApi.Enums;

namespace EventManagementApi.DTOs;

public record BookingResponseDto(
    Guid Id,
    Guid EventId,
    String Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);
