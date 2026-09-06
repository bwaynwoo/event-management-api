using BookingService.Application.DTOs;
using BookingService.Application.Repositories;
using BookingService.Domain.Exceptions;
using BookingService.Domain.Models;

namespace BookingService.Application.Services;

internal sealed class BookingService(IBookingRepository bookingRepository) : IBookingService
{
    private const int MaxActiveBookingsPerUser = 10;

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var activeCount = await bookingRepository.GetActiveCountByUserAsync(userId, cancellationToken);
        if (activeCount >= MaxActiveBookingsPerUser)
            throw new BookingLimitExceededException(userId, MaxActiveBookingsPerUser);

        var booking = Booking.CreatePending(eventId, userId);
        await bookingRepository.AddAsync(booking, cancellationToken);

        return ToInfo(booking);
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken)
                      ?? throw new NotFoundException("Booking not found");

        if (!isAdmin && booking.UserId != userId)
            throw new ForbiddenException("You can only cancel your own bookings");

        booking.Cancel();
        await bookingRepository.UpdateAsync(booking, cancellationToken);
    }


    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken)
                      ?? throw new NotFoundException("Booking not found");

        return ToInfo(booking);
    }

    private static BookingInfo ToInfo(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        UserId = booking.UserId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}