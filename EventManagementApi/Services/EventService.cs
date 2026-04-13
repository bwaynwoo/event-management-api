using EventManagementApi.Models;

namespace EventManagementApi.Services;

public class EventService : IEventService
{
    private static List<Event> Events { get; set; } = [];
    private static int _nextId = 1;

    public IReadOnlyCollection<Event> GetEvents()
    {
        return Events;
    }

    public Event? GetEvent(int id)
    {
        return Events.FirstOrDefault(e => e.Id == id);
    }

    public void AddEvent(Event eventItem)
    {
        eventItem.Id = _nextId++;
        Events.Add(eventItem);
    }

    public void UpdateEvent(int id, Event eventItem)
    {
        var existingEvent = Events.FirstOrDefault(e => e.Id == id);
        if (existingEvent != null)
        {
            existingEvent.Title = eventItem.Title;
            existingEvent.Description = eventItem.Description;
            existingEvent.StartAt = eventItem.StartAt;
            existingEvent.EndAt = eventItem.EndAt;
        }
    }

    public void RemoveEvent(int id)
    {
        var eventItem = Events.FirstOrDefault(e => e.Id == id);
        if (eventItem != null)
        {
            Events.Remove(eventItem);
        }
    }
}