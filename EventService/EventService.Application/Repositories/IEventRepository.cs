using EventService.Domain.Models;

namespace EventService.Application.Repositories;

public interface IEventRepository
{
    Task<Event> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Event>> GetTopAsync(int count, CancellationToken cancellationToken = default);
    Task AddAsync(Event @event, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<(List<Event> Items, int TotalCount)> GetEventsAsync(
        int page,
        int pageSize,
        DateTime? from,
        DateTime? to,
        string? title,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Event @event, CancellationToken cancellationToken = default);
    Task UpdateAsync(Event @event, CancellationToken cancellationToken = default);
}