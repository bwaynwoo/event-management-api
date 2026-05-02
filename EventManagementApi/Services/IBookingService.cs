namespace EventManagementApi.Services;

public interface IBookingService
{
    Task CreateBookingAsync(Guid eventId);
    Task GetBookingByIdAsync(Guid bookingId);
}