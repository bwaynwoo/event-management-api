namespace EventManagementApi.DTOs;

public record PaginatedResult<T>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<T> Items
);