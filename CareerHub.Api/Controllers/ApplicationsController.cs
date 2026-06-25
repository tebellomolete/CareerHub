using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;
using CareerHub.Api.Exceptions;
using Asp.Versioning;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}")]
[ApiVersion(1)]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpPost("jobs/{id}/applications")]
    [EnableRateLimiting("apply")]
    // [Authorize(Roles = "Applicant")]
    public async Task<IActionResult> SubmitApplication(Guid id, SubmitApplicationRequest request)
    {
        await _applicationService.SubmitApplicationAsync(id, request);
        return Ok(new { Message = "Application submitted successfully." });
    }

    [HttpGet("{listingId}/applicants/{applicantId}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetApplication(Guid listingId, Guid applicantId)
    {
        var application = await _applicationService.GetApplicationAsync(applicantId, listingId);
        if (application == null) return NotFound();

        var etag = $"\"{application.Status}\"";
        
        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304);
        }
        
        Response.Headers.ETag = etag;
        return Ok(application);
    }

    [HttpPatch("applications/{listingId}/applicants/{applicantId}/status")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> UpdateApplicationStatus(Guid listingId, Guid applicantId, UpdateApplicationStatusRequest request)
    {
        try
        {
            await _applicationService.UpdateApplicationStatusAsync(applicantId, listingId, request.Status);
            return Ok(new { Message = "Application status updated successfully.", Status = request.Status });
        }
        catch (InvalidStatusTransitionException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
