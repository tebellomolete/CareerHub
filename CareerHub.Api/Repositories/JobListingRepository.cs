using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public class JobListingRepository : IJobListingRepository
{
    private readonly CareerHubDbContext _context;

    public JobListingRepository(CareerHubDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<JobResponse>> GetActiveListingsAsync()
    {
        var jobsData = await _context.JobListings
            .AsNoTracking()
            .Where(j => j.IsActive)
            .Select(j => new 
            {
                j.Id,
                j.Title,
                CompanyName = j.Company.Name,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                j.SalaryMin,
                j.SalaryMax,
                ApplicationCount = j.Applications.Count
            })
            .ToListAsync();

        return jobsData.Select(j => 
        {
            string salaryDisplay = "Salary not specified";
            if (j.SalaryMin.HasValue && j.SalaryMax.HasValue)
                salaryDisplay = $"R{j.SalaryMin:N0} - R{j.SalaryMax:N0}/month";
            else if (j.SalaryMin.HasValue)
                salaryDisplay = $"From R{j.SalaryMin:N0}/month";

            return new JobResponse(
                j.Id,
                j.Title,
                j.CompanyName,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                salaryDisplay,
                j.ApplicationCount
            );
        });
    }

    public async Task<JobDetailResponse?> GetListingWithDetailsAsync(Guid id)
    {
        var jobData = await _context.JobListings
            .AsNoTracking()
            .Where(j => j.Id == id)
            .Select(j => new 
            {
                j.Id,
                j.Title,
                CompanyName = j.Company.Name,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                j.SalaryMin,
                j.SalaryMax,
                ApplicationCount = j.Applications.Count,
                Applications = j.Applications.Select(a => new ApplicationResponse(
                    a.Applicant.Name,
                    a.SubmittedAt,
                    a.Status
                )).ToList()
            })
            .FirstOrDefaultAsync();

        if (jobData == null) return null;

        string salaryDisplay = "Salary not specified";
        if (jobData.SalaryMin.HasValue && jobData.SalaryMax.HasValue)
            salaryDisplay = $"R{jobData.SalaryMin:N0} - R{jobData.SalaryMax:N0}/month";
        else if (jobData.SalaryMin.HasValue)
            salaryDisplay = $"From R{jobData.SalaryMin:N0}/month";

        return new JobDetailResponse(
            jobData.Id,
            jobData.Title,
            jobData.CompanyName,
            jobData.Location,
            jobData.Description,
            jobData.Type,
            jobData.PostedAt,
            salaryDisplay,
            jobData.ApplicationCount,
            jobData.Applications
        );
    }

    public async Task<JobListing?> GetListingByIdAsync(Guid id)
    {
        return await _context.JobListings.FindAsync(id);
    }

    public async Task<bool> IsOpenForApplicationsAsync(Guid id)
    {
        var listing = await _context.JobListings
            .AsNoTracking()
            .Select(j => new { j.Id, j.IsActive, j.ClosingDate })
            .FirstOrDefaultAsync(j => j.Id == id);
            
        if (listing == null) return false;
        
        return listing.IsActive && listing.ClosingDate > DateTime.UtcNow;
    }

    public async Task AddListingAsync(JobListing listing)
    {
        _context.JobListings.Add(listing);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateListingAsync(JobListing listing)
    {
        _context.JobListings.Update(listing);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteListingAsync(JobListing listing)
    {
        _context.JobListings.Remove(listing);
        await _context.SaveChangesAsync();
    }
}
