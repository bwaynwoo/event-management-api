using EventService.Application.DTOs;
using EventService.Application.Repositories;
using EventService.Domain.Models;
using Microsoft.Extensions.Options;

namespace EventService.Application.Services;

public sealed class EventService(
    IEventRepository eventRepository,
    ICacheService cache,
    IOptions<EventCacheOptions> cacheOptions)
    : IEventService
{
    public async Task<EventInfo> CreateEventAsync(CreateEvent request, CancellationToken cancellationToken = default)
    {
        var @event = Event.Create(request.Title, request.StartAt, request.EndAt, request.TotalSeats,
            request.Description);
        await eventRepository.AddAsync(@event, cancellationToken);
        await cache.RemoveAsync(CacheKeys.TopEvents, cancellationToken);

        return ToInfo(@event);
    }

    public async Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Event(id);

        var cached = await cache.GetAsync<EventInfo>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);
        var info = ToInfo(@event);

        await cache.SetAsync(cacheKey, info, cacheOptions.Value.EventTtl, cancellationToken);
        return info;
    }

    public async Task<IReadOnlyCollection<EventInfo>> GetTopEventsAsync(CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<EventInfo[]>(CacheKeys.TopEvents, cancellationToken);
        if (cached is not null)
            return cached;

        var events = await eventRepository.GetTopAsync(10, cancellationToken);
        var result = events.Select(ToInfo).ToArray();
        await cache.SetAsync(CacheKeys.TopEvents, result, cacheOptions.Value.TopEventsTtl, cancellationToken);

        return result;
    }

    public async Task<PaginatedResult<EventInfo>> GetAllEventsAsync(
        int page = 1,
        int pageSize = 10,
        DateTime? from = null,
        DateTime? to = null,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await eventRepository.GetEventsAsync(
            page, pageSize, from, to, title, cancellationToken);

        return new PaginatedResult<EventInfo>
        {
            Items = items.Select(ToInfo).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EventInfo> UpdateEventAsync(Guid id, UpdateEvent request,
        CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);

        @event.Update(request.Title, request.StartAt, request.EndAt, request.Description);
        await eventRepository.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(CacheKeys.Event(id), cancellationToken);
        await cache.RemoveAsync(CacheKeys.TopEvents, cancellationToken);

        return ToInfo(@event);
    }

    public async Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);

        await eventRepository.DeleteAsync(@event, cancellationToken);
        await cache.RemoveAsync(CacheKeys.Event(id), cancellationToken);
        await cache.RemoveAsync(CacheKeys.TopEvents, cancellationToken);
    }

    internal static EventInfo ToInfo(Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        StartAt = @event.StartAt,
        EndAt = @event.EndAt,
        TotalSeats = @event.TotalSeats,
        AvailableSeats = @event.AvailableSeats,
        Description = @event.Description
    };
}