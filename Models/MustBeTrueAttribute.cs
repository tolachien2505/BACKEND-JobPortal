using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models;

public class MustBeTrueAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is bool boolValue && boolValue)
        {
            return ValidationResult.Success;
        }
        return new ValidationResult(ErrorMessage ?? "Giá trị phải là true.");
    }
}
