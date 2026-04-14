using System.ComponentModel.DataAnnotations;
using EventManagementApi.Constants;

namespace EventManagementApi.DTOs;

public record EventRequestDto(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        List<ValidationResult> errors = [];

        if (StartAt == default)
        {
            errors.Add(new ValidationResult(string.Format(ErrorMessages.FieldIsRequired, nameof(StartAt))));
        }
        
        if (EndAt == default)
        {
            errors.Add(new ValidationResult(string.Format(ErrorMessages.FieldIsRequired, nameof(EndAt))));
        }
        
        if (EndAt <= StartAt)
        {
            errors.Add(new ValidationResult(ErrorMessages.EndAtMustBeLater));
        }
        return errors;
    }
}