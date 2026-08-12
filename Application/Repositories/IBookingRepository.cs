using Domain.Models;

namespace Application.Repositories;

public interface IBookingRepository
{
    Task<Booking> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetPendingBookingIds(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}