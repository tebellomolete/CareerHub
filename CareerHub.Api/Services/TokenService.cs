using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CareerHub.Api.Services;

// Assignment 2.4 — signs access tokens against the same
// symmetric key `Program.cs` already registered with the
// JwtBearer authentication handler.
//
// The claim set is:
//   - `sub`   → the account id (email)
//   - `email` → the account email (same value; kept as a first-
//               class claim because the Flutter `User` model
//               reads it by name, not by convention)
//   - `name`  → the display name
//   - `role`  → the account role (kept from the previous
//               controller so [Authorize(Roles = "…")] still
//               works in future assignments)
//   - `exp`   → 5 minutes from now
//   - `iat`   → issued-at (helpful for server-side rate limits)
//
// Lifetime is deliberately short so the refresh flow is
// exercisable during the assignment demo without waiting hours
// for the token to expire.
public sealed class TokenService : ITokenService
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string IssueAccessToken(UserAccount user)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.Add(AccessTokenLifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
