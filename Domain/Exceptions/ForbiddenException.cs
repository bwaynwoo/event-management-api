namespace Domain.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException(Guid userId, Guid bookingId)
        : base($"User {userId} does not have permission to modify booking {bookingId}.")
    {
    }
}