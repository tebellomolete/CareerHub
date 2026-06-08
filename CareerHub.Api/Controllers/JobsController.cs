namespace CareerHub.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly IJobListingService _jobService;

    public JobsController(IJobListingService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllJobs()
    {
        var response = await _jobService.GetActiveListingsAsync();
        return Ok(response);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery(Name = "q")] string term) => Ok(await _jobService.SearchAsync(term));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobById(Guid id)
    {
        var response = await _jobService.GetListingWithDetailsAsync(id);
        return Ok(response);
    }

    [HttpGet("company/{companyId:guid}/stats")]
    public async Task<ActionResult<IEnumerable<JobListingStatsResponse>>> GetCompanyStats(Guid companyId)
    {
        var stats = await _jobService.GetCompanyStatsAsync(companyId);
        return Ok(stats);
    }

    [HttpPost]
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

    [HttpDelete("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        await _jobService.DeleteListingAsync(id);
        return NoContent(); 
    }
}