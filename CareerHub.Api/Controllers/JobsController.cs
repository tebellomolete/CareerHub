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
        var jobs = await _context.JobListings.ToListAsync();
        return Ok(jobs.Select(JobResponse.FromListing));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobById(Guid id)
    {
        var job = await _context.JobListings.FindAsync(id);
        if (job == null)
        {
            throw new JobNotFoundException(id);
        }
        
        return Ok(JobResponse.FromListing(job));
    }

    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> CreateJob(CreateJobRequest request)
    {
        // Idempotency check: Case-insensitive duplicate check in database
        bool isDuplicate = await _context.JobListings.AnyAsync(j => 
            j.Title.ToLower() == request.Title.ToLower() &&
            j.Company.ToLower() == request.Company.ToLower());

        if (isDuplicate)
        {
            throw new DuplicateJobListingException(request.Company, request.Title);
        }

        var newJob = new JobListing
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Company = request.Company,
            Location = request.Location,
            Type = request.Type,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax
        }; // PostedAt and IsActive handled by defaults

        _context.JobListings.Add(newJob);
        await _context.SaveChangesAsync();

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
        existingJob.Company = request.Company;
        existingJob.Location = request.Location;
        existingJob.Type = request.Type;
        existingJob.SalaryMin = request.SalaryMin;
        existingJob.SalaryMax = request.SalaryMax;

        await _context.SaveChangesAsync();

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