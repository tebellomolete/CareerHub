namespace CareerHub.Api.Services;

// Assignment 2.4 — the very small user-lookup abstraction used by
// AuthController.Login. Backed by an in-memory hard-coded map in
// `InMemoryUserAccountStore` — no DB table, no migration. Kept as
// an interface so a future assignment can swap in an EF-backed
// implementation without touching the controller.
public interface IUserAccountStore
{
    // Returns the resolved account (id / email / display name /
    // role) if the credentials are valid, or null otherwise.
    UserAccount? Authenticate(string email, string password);

    // Look up by id (the JWT `sub` claim) so the refresh flow can
    // re-issue an access token without re-authenticating.
    UserAccount? FindById(string id);
}

public sealed record UserAccount(
    string Id,
    string Email,
    string DisplayName,
    string Role);
