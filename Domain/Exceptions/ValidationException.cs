namespace Domain.Exceptions
{
    public sealed class ValidationException : Exception
    {
        public IDictionary<string, ICollection<string>> Errors { get; } =
            new Dictionary<string, ICollection<string>>();

        public ValidationException(IDictionary<string, ICollection<string>> errors) : base("Validation failed")
        {
            Errors = errors;
        }

        public ValidationException(string field, string error) : base("Validation failed")
        {
            Errors = new Dictionary<string, ICollection<string>>
            {
                { field, new[] { error } }
            };
        }

        public ValidationProblemDetails ToProblemDetails()
            => new ValidationProblemDetails
            {
                Status = 400,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Errors = Errors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToArray())
            };
    }

    public class ValidationProblemDetails : ProblemDetails
    {
        public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
    }
}