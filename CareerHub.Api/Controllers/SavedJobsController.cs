using System.Security.Claims;
using Asp.Versioning;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerHub.Api.Controllers;

// Assignment 2.4 Stretch C — the tiny saved-jobs surface the
// mobile app's SavedJobsRepository targets.
//
// POST /api/v1/saved  { jobId } — 200 on success, 404 if the job
//                                 listing does not exist.
// GET  /api/v1/saved              — 200 { jobIds: [...] } — used
//                                 by the client to reconcile its
//                                 local cache after a fresh sign-in.
// DELETE /api/v1/saved/{jobId}   — 204 always (idempotent remove).
//
// Rate limiting isn't applied here; the stretch is a small
// bookkeeping surface, not a submission endpoint.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/saved")]
[Authorize]
public class SavedJobsController : ControllerBase
{
    private readonly ISavedJobsStore _store;

    public SavedJobsController(ISavedJobsStore store)
    {
        _store = store;
    }

    [HttpPost]
    public async Task<IActionResult> Save(
        [FromBody] SaveJobRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.JobId))
        {
            return BadRequest(new { message = "JobId is required." });
        }

        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _store.SaveAsync(userId, request.JobId, ct);
        return result switch
        {
            SaveResult.Saved => Ok(new { jobId = request.JobId }),
            SaveResult.NotFound => NotFound(new
            {
                message = "That job listing no longer exists.",
                jobId = request.JobId,
            }),
            _ => StatusCode(500),
        };
    }

    [HttpGet]
    public IActionResult List()
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        return Ok(new { jobIds = _store.List(userId) });
    }

    [HttpDelete("{jobId}")]
    public IActionResult Remove(string jobId)
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        _store.Remove(userId, jobId);
        return NoContent();
    }

    private string? ResolveUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
    }
}
