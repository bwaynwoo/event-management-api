namespace EventManagementApi.DTOs;

public record EventCreateUpdateDto(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
);