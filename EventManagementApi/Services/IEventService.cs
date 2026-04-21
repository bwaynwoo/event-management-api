using EventManagementApi.Models;

namespace EventManagementApi.Services;

public interface IEventService
{
    IReadOnlyCollection<Event> GetEvents(string? title = null, DateTime? from = null, DateTime? to = null);
    Event GetEvent(int id);
    void AddEvent(Event eventItem);
    void UpdateEvent(int id, Event eventItem);
    void RemoveEvent(int id);
}