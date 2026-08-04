using Application.Repositories;
using Domain.Exceptions;
using Domain.Models;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;

    public EventRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Event> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
               ?? throw new NotFoundException("Event not found");
    }

    public async Task AddAsync(Event @event, CancellationToken cancellationToken)
    {
        await _db.Events.AddAsync(@event, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<Event> Items, int TotalCount)> GetEventsAsync(
        int page,
        int pageSize,
        DateTime? from,
        DateTime? to,
        string? title,
        CancellationToken cancellationToken)
    {
        var query = _db.Events.AsQueryable();

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task DeleteAsync(Event @event, CancellationToken cancellationToken)
    {
        _db.Events.Remove(@event);
        await _db.SaveChangesAsync(cancellationToken);
    }
}