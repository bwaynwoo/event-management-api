using System.Collections.Concurrent;
using EventManagementApi.DTOs;
using EventManagementApi.Exceptions;
using EventManagementApi.Models;

namespace EventManagementApi.Services;

public class EventService : IEventService
{
    private static readonly ConcurrentDictionary<int, Event> Events = new();
    private static int _nextId = 1;

    public PaginatedResult<Event> GetEvents(GetEventsRequestDto dto)
    {
        var events = Events.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            events = events.Where(e =>
                e.Title.Contains(dto.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (dto.From.HasValue)
        {
            events = events.Where(e => e.StartAt >= dto.From.Value);
        }

        if (dto.To.HasValue)
        {
            events = events.Where(e => e.EndAt <= dto.To.Value);
        }

        var eventsList = events.ToList();

        var totalCount = eventsList.Count;

        var items = eventsList
            .Skip((dto.Page - 1) * dto.PageSize)
            .Take(dto.PageSize)
            .ToList()
            .AsReadOnly();

        return new PaginatedResult<Event>(dto.Page, dto.PageSize, totalCount, items);
    }

    public Event GetEvent(int id)
    {
        if (!Events.TryGetValue(id, out var eventItem))
        {
            throw new NotFoundException("Event", id);
        }

        return eventItem;
    }

    public void AddEvent(Event eventItem)
    {
        eventItem.Id = _nextId++;
        Events.TryAdd(eventItem.Id, eventItem);
    }

    public void UpdateEvent(int id, Event eventItem)
    {
        if (!Events.TryGetValue(id, out var oldEvent))
        {
            throw new NotFoundException("Event", id);
        }

        Events.TryUpdate(id, eventItem, oldEvent);
    }

    public void RemoveEvent(int id)
    {
        if (!Events.TryRemove(id, out _))
        {
            throw new NotFoundException("Event", id);
        }

        ;
    }
}