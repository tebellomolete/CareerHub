namespace CareerHub.Api.DTOs;

public record UpdateJobListingRequest(
    string? Title,
    string? Description,
    string? Location,
    string? EmploymentType,
    int? SalaryMin,
    int? SalaryMax,
    DateTime? ExpiresAt
);
