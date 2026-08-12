using Application.DTOs;
using Application.Repositories;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Models;

namespace Application.Services;

internal sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly SemaphoreSlim _bookingLock;
    private const int MaxActiveBookingsPerUser = 10;

    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository,
        SemaphoreSlim bookingLock)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _bookingLock = bookingLock;
    }

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _bookingLock.WaitAsync(cancellationToken);
        try
        {
            var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);

            if (@event.StartAt <= DateTime.UtcNow)
                throw new EventAlreadyStartedException(eventId);

            var userBookings = await _bookingRepository.GetByUserIdAsync(userId, cancellationToken);
            var activeBookings = userBookings.Count(b =>
                b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed);

            if (activeBookings >= MaxActiveBookingsPerUser)
                throw new BookingLimitExceededException(userId, MaxActiveBookingsPerUser);

            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            var booking = Booking.CreatePending(eventId, userId);
            await _bookingRepository.AddAsync(booking, cancellationToken);
            await _bookingRepository.SaveChangesAsync(cancellationToken);

            return ToInfo(booking);
        }
        finally
        {
            _bookingLock.Release();
        }
    }
    
    public async Task CancelBookingAsync(Guid bookingId, Guid userId, Role userRole, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);

        if (booking.UserId != userId && userRole != Role.Admin)
            throw new ForbiddenException(userId, bookingId);

        if (booking.Status != BookingStatus.Pending)
            throw new ValidationException("Booking", $"Cannot cancel booking with status '{booking.Status}'.");

        booking.Cancel();

        var @event = await _eventRepository.GetByIdAsync(booking.EventId, cancellationToken);
        @event.ReleaseSeats();

        await _bookingRepository.SaveChangesAsync(cancellationToken);
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