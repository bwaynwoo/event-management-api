using BookingService.Domain.Models;

namespace BookingService.Application.Repositories;

public interface IBookingRepository
{
    Task<Booking> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<int> GetActiveCountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default);
}