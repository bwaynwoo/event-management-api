using BookingService.Application.Repositories;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;
using BookingService.Domain.Models;
using BookingService.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Repositories;

public class BookingRepository(AppDbContext db) : IBookingRepository
{
    public async Task<Booking> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        return await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
               ?? throw new NotFoundException("Booking not found");
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        await db.Bookings.AddAsync(booking, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetPendingAsync(CancellationToken cancellationToken)
    {
        return await db.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveCountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Bookings
            .Where(b => b.UserId == userId &&
                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .CountAsync(cancellationToken);
    }

    public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        db.Bookings.Update(booking);
        await db.SaveChangesAsync(cancellationToken);
    }
}