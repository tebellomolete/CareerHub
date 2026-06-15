namespace CareerHub.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;
using CareerHub.Api.Models;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1)]
public class JobsController : ControllerBase
{
    private readonly IJobListingService _jobService;

    public JobsController(IJobListingService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet]
    [EndpointSummary("List all jobs")]
    [EndpointDescription(
        "Returns a paginated list of jobs ordered by PostedAt. " +
        "The X-Total-Count response header contains the total number of jobs " +
        "matching the current filter, regardless of page size.")]
    public async Task<IActionResult> GetAllJobs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] JobListingFilterQuery filter = null)
    {
        filter ??= new JobListingFilterQuery();
        var response = await _jobService.GetActiveListingsPagedAsync(page, pageSize, filter);
        Response.Headers.Append("X-Total-Count", response.TotalCount.ToString());
        return Ok(response);
    }

    [HttpGet("company/{companyId:guid}")]
    public async Task<IActionResult> GetCompanyListings(Guid companyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] JobListingFilterQuery filter = null)
    {
        filter ??= new JobListingFilterQuery();
        var companyFilter = filter with { CompanyId = companyId };
        var response = await _jobService.GetActiveListingsPagedAsync(page, pageSize, companyFilter);
        Response.Headers.Append("X-Total-Count", response.TotalCount.ToString());
        return Ok(response);
    }

    [HttpGet("search")]
    [EnableRateLimiting("search")]
    [EndpointSummary("Search jobs")]
    [EndpointDescription(
        "Filter jobs by full-text query. " +
        "When Q is provided, PostgreSQL full-text search is used with GIN index support. " +
        "Rate limited to 20 requests per minute — full-text queries are expensive.")]
    public async Task<IActionResult> Search([FromQuery(Name = "q")] string term) => Ok(await _jobService.SearchAsync(term));

    [HttpGet("{id}")]
    [EndpointSummary("Get a job listing by id")]
    [EndpointDescription(
        "Returns a single job listing by id. " +
        "Fingerprint from the fields most likely to change. " +
        "If PostedAt or Salary change, the ETag changes and the client re-fetches.")]
    public async Task<IActionResult> GetJobById(Guid id)
    {
        var response = await _jobService.GetListingWithDetailsAsync(id);
        if (response == null) return NotFound();

        // Part 7: ETags
        var etag = $"\"{id}-{response.PostedAt.Ticks}-{response.SalaryDisplay.GetHashCode()}\"";
        
        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304);
        }
        
        Response.Headers.ETag = etag;
        return Ok(response);
    }

    [HttpGet("company/{companyId:guid}/stats")]
    public async Task<ActionResult<IEnumerable<JobListingStatsResponse>>> GetCompanyStats(Guid companyId)
    {
        var stats = await _jobService.GetCompanyStatsAsync(companyId);
        return Ok(stats);
    }

    [HttpPost]
    [EnableRateLimiting("post-listing")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> CreateJob(CreateJobRequest request)
    {
        var response = await _jobService.CreateListingAsync(request);
        return CreatedAtAction(nameof(GetJobById), new { id = response.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> UpdateJob(Guid id, UpdateJobRequest request)
    {
        var response = await _jobService.UpdateListingAsync(id, request);
        return Ok(response);
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "Employer")]
    
    public async Task<IActionResult> PatchJob(Guid id, UpdateJobListingRequest request)
    {
        var response = await _jobService.PatchAsync(id, request);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        await _jobService.DeleteListingAsync(id);
        return NoContent(); 
    }
}