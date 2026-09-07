namespace BookingService.Domain.Exceptions;

public class BookingLimitExceededException(Guid userId, int limit)
    : Exception($"User {userId} has reached the maximum limit of {limit} active bookings.");