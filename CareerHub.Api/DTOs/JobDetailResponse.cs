namespace CareerHub.Api.DTOs;

using CareerHub.Api.Models;

public record ApplicationResponse(
    string ApplicantName,
    DateTime SubmittedAt,
    ApplicationStatus Status
);

public record JobDetailResponse(
    Guid Id,
    string Title,
    string CompanyName,
    string Location,
    string Description,
    JobType Type,
    DateTime PostedAt,
    string SalaryDisplay,
    int ApplicationCount,
    List<ApplicationResponse> Applications
) : JobResponse(Id, Title, CompanyName, Location, Description, Type, PostedAt, SalaryDisplay, ApplicationCount);
