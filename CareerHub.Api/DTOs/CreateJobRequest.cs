namespace CareerHub.Api.DTOs;

using System.ComponentModel.DataAnnotations;
using CareerHub.Api.Models;

public record CreateJobRequest : IValidatableObject
{
    [Required]
    [StringLength(120, MinimumLength = 5)] 
    public string Title { get; init; } = default!;

    [Required]
    [StringLength(80, MinimumLength = 2)] 
    public string Company { get; init; } = default!;

    [Required] 
    public string Location { get; init; } = default!;

    [Required]
    [MinLength(20)] 
    public string Description { get; init; } = default!;

    [Required] 
    public JobType Type { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Salary minimum must be greater than zero.")] 
    public int? SalaryMin { get; init; } = null;

    [Range(1, int.MaxValue, ErrorMessage = "Salary maximum must be greater than zero.")] 
    public int? SalaryMax { get; init; } = null;

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