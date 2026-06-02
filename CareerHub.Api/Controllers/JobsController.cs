namespace CareerHub.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using CareerHub.Api.Exceptions;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly JobStore _jobStore;

    public JobsController(JobStore jobStore)
    {
        _jobStore = jobStore;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllJobs()
    {
        var jobs = await _jobStore.GetAllJobsAsync();
        return Ok(jobs.Select(JobResponse.FromListing));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobById(Guid id)
    {
        var job = await _jobStore.GetJobByIdAsync(id);
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
        var allJobs = await _jobStore.GetAllJobsAsync();
        
        // Idempotency check: Case-insensitive duplicate check
        bool isDuplicate = allJobs.Any(j => 
            j.Title.Equals(request.Title, StringComparison.OrdinalIgnoreCase) &&
            j.Company.Equals(request.Company, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            throw new DuplicateJobListingException(request.Company, request.Title);
        }

        var newJob = new JobListing(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.Company,
            request.Location,
            request.Type,
            request.SalaryMin,
            request.SalaryMax
        ); // PostedAt and IsActive handled by the record's init properties

        await _jobStore.AddJobAsync(newJob);

        var response = JobResponse.FromListing(newJob);
        
        return CreatedAtAction(nameof(GetJobById), new { id = newJob.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> UpdateJob(Guid id, UpdateJobRequest request)
    {
        var existingJob = await _jobStore.GetJobByIdAsync(id);
        if (existingJob == null)
        {
            throw new JobNotFoundException(id);
        }

        // Using 'with' creates a new record, safely keeping PostedAt and IsActive intact
        var updatedJob = existingJob with
        {
            Title = request.Title,
            Description = request.Description,
            Company = request.Company,
            Location = request.Location,
            Type = request.Type,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax
        };

        await _jobStore.UpdateJobAsync(updatedJob);

        return Ok(JobResponse.FromListing(updatedJob));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var existingJob = await _jobStore.GetJobByIdAsync(id);
        if (existingJob == null)
        {
            throw new JobNotFoundException(id);
        }

        await _jobStore.DeleteJobAsync(id);
        
        return NoContent(); 
    }
}