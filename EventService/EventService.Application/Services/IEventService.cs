using EventService.Application.DTOs;

namespace EventService.Application.Services;

public interface IEventService
{
    Task<PaginatedResult<EventInfo>> GetAllEventsAsync(int page = 1, int pageSize = 10, DateTime? from = null,
        DateTime? to = null, string? title = null, CancellationToken cancellationToken = default);

    Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventInfo> CreateEventAsync(CreateEvent createEvent, CancellationToken cancellationToken = default);
    Task<EventInfo> UpdateEventAsync(Guid id, UpdateEvent updateEvent, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);
}