namespace CareerHub.Api.DTOs;

using CareerHub.Api.Models;

public record UpdateJobRequest(
    string Title,
    string Company,
    string Location,
    string Description,
    JobType Type,
    int? SalaryMin = null,
    int? SalaryMax = null
) : CreateJobRequest(Title, Company, Location, Description, Type, SalaryMin, SalaryMax);