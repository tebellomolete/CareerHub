namespace CareerHub.Api.DTOs;

using CareerHub.Api.Models;

public record JobResponse(
    Guid Id,
    string Title,
    string Company,
    string Location,
    string Description,
    JobType Type,
    DateTime PostedAt,
    string SalaryDisplay
)
{
    // Helper method to handle mapping and the SalaryDisplay string
    public static JobResponse FromListing(JobListing job)
    {
        string salaryDisplay = "Salary not specified";
        
        if (job.SalaryMin.HasValue && job.SalaryMax.HasValue)
        {
            salaryDisplay = $"R{job.SalaryMin:N0} - R{job.SalaryMax:N0}/month";
        }
        else if (job.SalaryMin.HasValue)
        {
            salaryDisplay = $"From R{job.SalaryMin:N0}/month";
        }

        return new JobResponse(
            job.Id,
            job.Title,
            job.Company,
            job.Location,
            job.Description,
            job.Type,
            job.PostedAt,
            salaryDisplay
        );
    }
}