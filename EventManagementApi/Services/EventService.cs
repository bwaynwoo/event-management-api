using System.Collections.Concurrent;
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

    public Event? GetEvent(int id)
    {
        Events.TryGetValue(id, out var eventItem);
        return eventItem;
    }

    public void AddEvent(Event eventItem)
    {
        eventItem.Id = _nextId++;
        Events.TryAdd(eventItem.Id,  eventItem);
    }

    public void UpdateEvent(int id, Event eventItem)
    {
        Events.TryUpdate(id, eventItem, Events[id]);
    }

    public void RemoveEvent(int id)
    {
        Events.TryRemove(id, out _);
    }
}