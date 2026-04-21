using System.Collections.Concurrent;
using EventManagementApi.Exceptions;
using EventManagementApi.Models;

namespace EventManagementApi.Services;

public class EventService : IEventService
{
    private static readonly ConcurrentDictionary<int, Event> Events = new();
    private static int _nextId = 1;

    public IReadOnlyCollection<Event> GetEvents()
    {
        return Events.Values.ToList();
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
        Events.TryAdd(eventItem.Id,  eventItem);
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
        if(!Events.TryRemove(id, out _))
        {
            throw new NotFoundException("Event", id);
        };
    }
}