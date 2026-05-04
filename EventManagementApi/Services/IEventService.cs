using EventManagementApi.DTOs;
using EventManagementApi.Models;

namespace EventManagementApi.Services;

public interface IEventService
{
    PaginatedResult<Event> GetEvents(GetEventsRequestDto dto);
    Event GetEvent(Guid id);
    void AddEvent(Event eventItem);
    void UpdateEvent(Guid id, Event eventItem);
    void RemoveEvent(Guid id);
    void Clear();
}