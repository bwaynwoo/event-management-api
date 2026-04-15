using EventManagementApi.Models;

namespace EventManagementApi.Services;

public interface IEventService
{
    IReadOnlyCollection<Event> GetEvents();
    Event? GetEvent(int id);
    void AddEvent(Event eventItem);
    void UpdateEvent(int id, Event eventItem);
    void RemoveEvent(int id);
}