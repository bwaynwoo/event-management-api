using Domain.Models;

namespace Application.Repositories;

public interface IEventRepository
{
    Task<Event> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);
    Task AddAsync(Event @event, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<(List<Event> Items, int TotalCount)> GetEventsAsync(
        int page,
        int pageSize,
        DateTime? from,
        DateTime? to,
        string? title,
        CancellationToken cancellationToken);
    
    Task DeleteAsync(Event @event, CancellationToken cancellationToken);
}