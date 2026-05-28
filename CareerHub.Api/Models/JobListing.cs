namespace CareerHub.Api.Models;

public record JobListing
(
    Guid Id,
    string Title,
    string Description,
    string Company,
    string Location,
    JobType Type,
    int? SalaryMin = null,
    int? SalaryMax = null
)
{
    // Server-owned fields initialized automatically upon creation
    public DateTime PostedAt { get; init; } = DateTime.UtcNow;
    public bool IsActive { get; init; } = true;
}