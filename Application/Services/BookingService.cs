using Application.DTOs;
using Application.Repositories;
using Domain.Exceptions;
using Domain.Models;

namespace Application.Services;

internal sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly SemaphoreSlim _bookingLock;

    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository,
        SemaphoreSlim bookingLock)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _bookingLock = bookingLock;
    }

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _bookingLock.WaitAsync(cancellationToken);
        try
        {
            var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);

            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
            
            if (@event.StartAt <= DateTime.UtcNow)
                throw new EventAlreadyStartedException(eventId);

            var booking = Booking.CreatePending(eventId, userId);
            await _bookingRepository.AddAsync(booking, cancellationToken);

            return ToInfo(booking);
        }
        finally
        {
            _bookingLock.Release();
        }
    }

    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);

        return ToInfo(booking);
    }

    internal static BookingInfo ToInfo(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        UserId = booking.UserId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}