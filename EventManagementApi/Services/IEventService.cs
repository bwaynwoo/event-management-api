using EventManagementApi.Models;

namespace EventManagementApi.Services;

public interface IEventService
{
    List<Event> GetEvents();
    Event? GetEvent(int id);
    void AddEvent(Event eventItem);
    void ChangeEvent(int id, Event eventItem);
    void RemoveEvent(int id);
}