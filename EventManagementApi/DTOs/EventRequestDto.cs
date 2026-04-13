namespace EventManagementApi.DTOs;

public record EventRequestDto(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
);