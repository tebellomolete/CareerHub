namespace CareerHub.Api.DTOs;

using System.ComponentModel.DataAnnotations;
using CareerHub.Api.Models;

public record CreateJobRequest(
    [property: Required]
    [property: StringLength(120, MinimumLength = 5)] 
    string Title,

    [property: Required]
    [property: StringLength(80, MinimumLength = 2)] 
    string Company,

    [property: Required] 
    string Location,

    [property: Required]
    [property: MinLength(20)] 
    string Description,

    [property: Required] 
    JobType Type,

    [property: Range(1, int.MaxValue, ErrorMessage = "Salary minimum must be greater than zero.")] 
    int? SalaryMin = null,

    [property: Range(1, int.MaxValue, ErrorMessage = "Salary maximum must be greater than zero.")] 
    int? SalaryMax = null
) : IValidatableObject
{
    // The cross-field validation works exactly the same in a record
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalaryMin.HasValue && SalaryMax.HasValue && SalaryMax <= SalaryMin)
        {
            yield return new ValidationResult(
                "SalaryMax must be greater than SalaryMin.",
                new[] { nameof(SalaryMax) }
            );
        }
    }
}