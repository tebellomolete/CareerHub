using System.Collections.Concurrent;

namespace CareerHub.Api.Services;

// Assignment 2.4 — the two hardcoded identities the mobile app can
// sign in with. Emails are used for authentication and are stored
// verbatim as the JWT `sub` claim so refresh doesn't need a
// separate id column. This is intentionally the same shape the
// previous `Username`-only Login had, just email-shaped strings
// and enriched with a `Name` field so the mobile app has a
// non-email display name.
public sealed class InMemoryUserAccountStore : IUserAccountStore
{
    // Case-insensitive email lookup — an operator typing
    // "Employer@CareerHub.dev" should authenticate successfully.
    private static readonly ConcurrentDictionary<string, (UserAccount Account, string Password)> Accounts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["employer@careerhub.dev"] = (
                new UserAccount(
                    Id: "employer@careerhub.dev",
                    Email: "employer@careerhub.dev",
                    DisplayName: "Employer",
                    Role: "Employer"),
                "password123"),
            ["applicant@careerhub.dev"] = (
                new UserAccount(
                    Id: "applicant@careerhub.dev",
                    Email: "applicant@careerhub.dev",
                    DisplayName: "Applicant",
                    Role: "Applicant"),
                "password123"),
        };

    public UserAccount? Authenticate(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        if (!Accounts.TryGetValue(email, out var entry))
        {
            return null;
        }

        // Constant-time-ish string compare. Acceptable for two
        // hardcoded accounts; a real password store would hash.
        return entry.Password == password ? entry.Account : null;
    }

    public UserAccount? FindById(string id)
    {
        return Accounts.TryGetValue(id, out var entry) ? entry.Account : null;
    }
}
