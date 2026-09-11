using EventService.Application.Repositories;
using EventService.Domain.Exceptions;
using EventService.Domain.Models;
using EventService.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Repositories;

public class EventRepository(AppDbContext db) : IEventRepository
{
    public async Task<Event> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
               ?? throw new NotFoundException("Event not found");
    }

    public async Task AddAsync(Event @event, CancellationToken cancellationToken)
    {
        await db.Events.AddAsync(@event, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<Event> Items, int TotalCount)> GetEventsAsync(
        int page,
        int pageSize,
        DateTime? from,
        DateTime? to,
        string? title,
        CancellationToken cancellationToken)
    {
        var query = db.Events.AsQueryable();

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

    public async Task<IReadOnlyCollection<Event>> GetTopAsync(int count, CancellationToken cancellationToken = default)
    {
        return await db.Events
            .Where(e => e.TotalSeats > 0)
            .OrderByDescending(e => (e.TotalSeats - e.AvailableSeats) * 1.0 / e.TotalSeats)
            .Take(count)
            .ToArrayAsync(cancellationToken);
    }

    public async Task DeleteAsync(Event @event, CancellationToken cancellationToken)
    {
        db.Events.Remove(@event);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        db.Events.Update(@event);
        await db.SaveChangesAsync(cancellationToken);
    }
}