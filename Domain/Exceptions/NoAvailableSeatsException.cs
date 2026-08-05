namespace Domain.Exceptions;

public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message) : base(message)
    {
    }

    public NoAvailableSeatsException()
        : base("No available seats for this event")
    {
    }
}