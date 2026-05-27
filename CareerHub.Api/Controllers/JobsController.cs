namespace CareerHub.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using CareerHub.Api.Data;

[ApiController]
[Route("jobs")]
public class JobsController : ControllerBase
{
    private readonly JobStore _jobStore;

    public JobsController(JobStore jobStore)
    {
        _jobStore = jobStore;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        var jobs = await _jobStore.GetAllJobsAsync();
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id)
    {
        var job = await _jobStore.GetJobByIdAsync(id);
        
        if (job == null)
        {
            return NotFound(); 
        }
        
        return Ok(job); 
    }
}