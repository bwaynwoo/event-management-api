using EventManagementApi.DTOs;
using EventManagementApi.Models;

namespace EventManagementApi.Services;

public interface IEventService
{
    PaginatedResult<Event> GetEvents(GetEventsRequestDto dto);
    Event GetEvent(int id);
    void AddEvent(Event eventItem);
    void UpdateEvent(int id, Event eventItem);
    void RemoveEvent(int id);
}