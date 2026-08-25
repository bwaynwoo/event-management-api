namespace Domain.Exceptions;

public class BookingLimitExceededException : Exception
{
    public BookingLimitExceededException(Guid userId, int limit)
        : base($"User {userId} has reached the maximum limit of {limit} active bookings.")
    {
    }
}