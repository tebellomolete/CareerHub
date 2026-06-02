using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareerHub.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Validate the credentials against hardcoded pair
        if (request.Username != "employer" || request.Password != "password123")
        {
            return Unauthorized();
        }

        // Retrieve JWT Secret
        var secret = _configuration["Jwt:Secret"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("JWT Secret is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Build JWT with subject (sub) and role claims
        // The role claim is set to "Employer" (or modified temporarily for testing)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(ClaimTypes.Role, "Employer") // Change to "User" temporarily for Part 5 verification
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(tokenString));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        // Retrieve username and role from user claims
        var username = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");

        return Ok(new
        {
            Username = username,
            Role = role
        });
    }
}
