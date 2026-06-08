using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpPost("{id}/applications")]
    [Authorize(Roles = "Applicant")]
    public async Task<IActionResult> SubmitApplication(Guid id, SubmitApplicationRequest request)
    {
        await _applicationService.SubmitApplicationAsync(id, request);
        return Ok(new { Message = "Application submitted successfully." });
    }
}
