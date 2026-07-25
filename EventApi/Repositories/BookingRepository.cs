using EventApi.DataAccess;
using EventApi.Exceptions;
using EventApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventApi.Repositories;

internal class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

    internal BookingRepository (AppDbContext db)
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
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}