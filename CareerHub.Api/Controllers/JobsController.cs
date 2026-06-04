namespace CareerHub.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using CareerHub.Api.Exceptions;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly CareerHubDbContext _context;

    public JobsController(CareerHubDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllJobs()
    {
        var jobsData = await _context.JobListings
            .AsNoTracking()
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

        var response = jobsData.Select(j => 
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

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobById(Guid id)
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
                ApplicationCount = j.Applications.Count
            })
            .FirstOrDefaultAsync();

        if (jobData == null)
        {
            throw new JobNotFoundException(id);
        }
        
        string salaryDisplay = "Salary not specified";
        if (jobData.SalaryMin.HasValue && jobData.SalaryMax.HasValue)
            salaryDisplay = $"R{jobData.SalaryMin:N0} - R{jobData.SalaryMax:N0}/month";
        else if (jobData.SalaryMin.HasValue)
            salaryDisplay = $"From R{jobData.SalaryMin:N0}/month";

        var response = new JobResponse(
            jobData.Id,
            jobData.Title,
            jobData.CompanyName,
            jobData.Location,
            jobData.Description,
            jobData.Type,
            jobData.PostedAt,
            salaryDisplay,
            jobData.ApplicationCount
        );

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> CreateJob(CreateJobRequest request)
    {
        bool isDuplicate = await _context.JobListings.AnyAsync(j => 
            j.Title.ToLower() == request.Title.ToLower() &&
            j.CompanyId == request.CompanyId);

        if (isDuplicate)
        {
            var company = await _context.Companies.FindAsync(request.CompanyId);
            string companyName = company?.Name ?? request.CompanyId.ToString();
            throw new DuplicateJobListingException(companyName, request.Title);
        }

        var newJob = new JobListing
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            CompanyId = request.CompanyId,
            Location = request.Location,
            Type = request.Type,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax
        };

        _context.JobListings.Add(newJob);
        await _context.SaveChangesAsync();

        // For the response, we might need the company name
        await _context.Entry(newJob).Reference(j => j.Company).LoadAsync();
        var response = JobResponse.FromListing(newJob);
        
        return CreatedAtAction(nameof(GetJobById), new { id = newJob.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> UpdateJob(Guid id, UpdateJobRequest request)
    {
        var existingJob = await _context.JobListings.FindAsync(id);
        if (existingJob == null)
        {
            throw new JobNotFoundException(id);
        }

        existingJob.Title = request.Title;
        existingJob.Description = request.Description;
        existingJob.CompanyId = request.CompanyId;
        existingJob.Location = request.Location;
        existingJob.Type = request.Type;
        existingJob.SalaryMin = request.SalaryMin;
        existingJob.SalaryMax = request.SalaryMax;

        await _context.SaveChangesAsync();

        await _context.Entry(existingJob).Reference(j => j.Company).LoadAsync();
        return Ok(JobResponse.FromListing(existingJob));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var existingJob = await _context.JobListings.FindAsync(id);
        if (existingJob == null)
        {
            throw new JobNotFoundException(id);
        }

        _context.JobListings.Remove(existingJob);
        await _context.SaveChangesAsync();
        
        return NoContent(); 
    }
}