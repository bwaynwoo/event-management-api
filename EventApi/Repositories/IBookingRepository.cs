using EventApi.Models;

namespace EventApi.Repositories;

internal interface IBookingRepository
{
    Task<Booking> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetPendingBookingIds(CancellationToken cancellationToken);
}