using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

public record JobListingFilterQuery(
    string? Location = null,
    string? EmploymentType = null,
    decimal? SalaryMin = null,
    decimal? SalaryMax = null,
    Guid? CompanyId = null,
    string Sort = "postedAt",
    string? Dir = null
);
