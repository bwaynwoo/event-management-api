namespace EventManagementApi.DTOs;

public record EventResponseDto(
    int Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
);