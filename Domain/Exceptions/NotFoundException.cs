namespace Domain.Exceptions
{
    public sealed class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }

        public ProblemDetails ToProblemDetails()
            => new ProblemDetails
            {
                Status = 404,
                Title = "Resource Not Found",
                Detail = Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            };
    }
    
    public class ProblemDetails
    {
        public int Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}