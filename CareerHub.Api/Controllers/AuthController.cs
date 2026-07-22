using Asp.Versioning;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerHub.Api.Controllers;

// Assignment 2.4 — the auth surface the mobile app depends on.
//
// Two changes from the previous shape:
//   1. Route is now versioned — `api/v{version:apiVersion}/auth` —
//      so the Flutter app's baseUrl of `http://.../api/v1` resolves
//      to both `/api/v1/auth/login` and the versioned data
//      endpoints without a special case.
//   2. Login returns an access-token + refresh-token pair, and a
//      new `/refresh` action rotates the refresh token per
//      Question 3 of README 2.4.
//
// The `/me` action is kept unchanged for parity with earlier
// assignments.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserAccountStore _users;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly ITokenService _tokens;

    public AuthController(
        IUserAccountStore users,
        IRefreshTokenStore refreshTokens,
        ITokenService tokens)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _tokens = tokens;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var account = _users.Authenticate(request.Email, request.Password);
        if (account is null)
        {
            return Unauthorized();
        }

        var accessToken = _tokens.IssueAccessToken(account);
        var refreshToken = _refreshTokens.Issue(account.Id);

        return Ok(new LoginResponse(accessToken, refreshToken));
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Unauthorized();
        }

        // Atomic rotate — the old token is invalidated as part of
        // the swap. If two calls race with the same token, exactly
        // one wins and the other gets `null` here.
        var rotation = _refreshTokens.Rotate(request.RefreshToken);
        if (rotation is null)
        {
            return Unauthorized();
        }

        var account = _users.FindById(rotation.UserId);
        if (account is null)
        {
            // The refresh token pointed at a user that no longer
            // exists. Revoke the freshly-issued rotation so a
            // subsequent refresh with the new token fails too.
            _refreshTokens.Revoke(rotation.NewToken);
            return Unauthorized();
        }

        var accessToken = _tokens.IssueAccessToken(account);
        return Ok(new LoginResponse(accessToken, rotation.NewToken));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout([FromBody] RefreshRequest request)
    {
        // Best-effort revoke. A client-side logout that skips this
        // is still safe — the token expires 30 days out — but calling
        // it lets us drop the entry from the store immediately.
        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            _refreshTokens.Revoke(request.RefreshToken);
        }
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;

        return Ok(new
        {
            Id = sub,
            Email = email,
            DisplayName = name,
            Role = role,
        });
    }
}
