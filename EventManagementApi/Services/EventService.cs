using EventManagementApi.Models;

namespace EventManagementApi.Services;

public class EventService : IEventService
{
    // Хранилище в памяти (имитация базы данных)
    private readonly List<Event> _events = new();
    private readonly object _lock = new(); // Для потокобезопасности

    public Task<IEnumerable<Event>> GetAllAsync()
    {
        return Task.FromResult(_events.AsEnumerable());
    }

    public Task<Event?> GetByIdAsync(Guid id)
    {
        var eventItem = _events.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(eventItem);
    }

    public Task<Event> CreateAsync(Event eventItem)
    {
        lock (_lock)
        {
            eventItem.Id = Guid.NewGuid();
            _events.Add(eventItem);
        }
        return Task.FromResult(eventItem);
    }

    public Task<Event?> UpdateAsync(Guid id, Event updatedEvent)
    {
        lock (_lock)
        {
            var existingEvent = _events.FirstOrDefault(e => e.Id == id);
            if (existingEvent == null)
            {
                return Task.FromResult<Event?>(null);
            }

            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartAt = updatedEvent.StartAt;
            existingEvent.EndAt = updatedEvent.EndAt;

            return Task.FromResult<Event?>(existingEvent);
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            var eventItem = _events.FirstOrDefault(e => e.Id == id);
            if (eventItem == null)
            {
                return Task.FromResult(false);
            }

            _events.Remove(eventItem);
            return Task.FromResult(true);
        }
    }
}