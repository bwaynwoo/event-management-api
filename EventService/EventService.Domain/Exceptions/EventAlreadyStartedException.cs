namespace EventService.Domain.Exceptions;

public class EventAlreadyStartedException : Exception
{
    public EventAlreadyStartedException(Guid eventId)
        : base($"Cannot book event {eventId}: the event has already started.")
    {
    }
}