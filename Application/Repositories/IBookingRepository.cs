using Domain.Models;

namespace Application.Repositories;

public interface IBookingRepository
{
    Task<Booking> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetPendingBookingIds(CancellationToken cancellationToken);
}