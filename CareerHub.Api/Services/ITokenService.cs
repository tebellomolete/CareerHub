namespace CareerHub.Api.Services;

// Assignment 2.4 — access-token issuance abstraction. The single
// place that owns the claim set and the 5-minute lifetime so the
// login and refresh code paths cannot drift apart.
public interface ITokenService
{
    string IssueAccessToken(UserAccount user);
}
