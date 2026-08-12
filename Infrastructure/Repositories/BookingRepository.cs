using Application.Repositories;
using Domain.Enums;
using Domain.Models;
using Domain.Exceptions;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

    public BookingRepository (AppDbContext db)
    {
        _db = db;
    }

    public async Task<Booking> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        return await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking not found");
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        await _db.Bookings.AddAsync(booking, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingBookingIds(CancellationToken cancellationToken)
    {
        return await _db.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.Bookings
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);
    }
    
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}