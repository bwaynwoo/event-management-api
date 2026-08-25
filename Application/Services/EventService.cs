using Application.DTOs;
using Application.Repositories;
using Domain.Models;

namespace Application.Services;

internal sealed class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;

    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<EventInfo> CreateEventAsync(CreateEvent request, CancellationToken cancellationToken = default)
    {
        var @event = Event.Create(request.Title, request.StartAt, request.EndAt, request.TotalSeats,
            request.Description);
        await _eventRepository.AddAsync(@event, cancellationToken);
        return ToInfo(@event);
    }

    public async Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken);

        return ToInfo(@event);
    }

    public async Task<PaginatedResult<EventInfo>> GetAllEventsAsync(
        int page = 1,
        int pageSize = 10,
        DateTime? from = null,
        DateTime? to = null,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _eventRepository.GetEventsAsync(
            page, pageSize, from, to, title, cancellationToken);

        return new PaginatedResult<EventInfo>
        {
            Items = items.Select(ToInfo).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EventInfo> UpdateEventAsync(Guid id, UpdateEvent request,
        CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken);

        @event.Update(request.Title, request.StartAt, request.EndAt, request.Description);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        return ToInfo(@event);
    }

    public async Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken);

        await _eventRepository.DeleteAsync(@event, cancellationToken);
    }

    internal static EventInfo ToInfo(Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        StartAt = @event.StartAt,
        EndAt = @event.EndAt,
        TotalSeats = @event.TotalSeats,
        AvailableSeats = @event.AvailableSeats,
        Description = @event.Description
    };
}