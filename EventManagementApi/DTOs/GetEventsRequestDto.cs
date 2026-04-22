using System.ComponentModel.DataAnnotations;

namespace EventManagementApi.DTOs;

public record GetEventsRequestDto : IValidatableObject
{
    public string? Title { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();

        if (Page < 1)
        {
            errors.Add(new ValidationResult("Page must be greater than or equal to 1", [nameof(Page)]));
        }

        if (PageSize < 1)
        {
            errors.Add(new ValidationResult("PageSize must be greater than or equal to 1", [nameof(PageSize)]));
        }

        if (From.HasValue && To.HasValue && From > To)
        {
            errors.Add(new ValidationResult("'From' date cannot be later than 'To' date", [nameof(From), nameof(To)]));
        }

        return errors;
    }
}