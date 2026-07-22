namespace CareerHub.Api.Services;

// Assignment 2.4 — the refresh-token store abstraction. See
// Question 3 in README 2.4 for the rotation semantics.
//
// `Issue(userId)` — creates a new opaque token, stores it, and
// returns the token string.
// `Rotate(oldToken)` — atomically removes the old token from the
// store and inserts a fresh one. Returns the new token + owning
// userId on success; returns null on any failure (unknown token,
// expired, or already rotated). This atomicity is what prevents
// the "three parallel refreshes" scenario from Q3 from succeeding
// more than once.
// `Revoke(token)` — removes without issuing a replacement. Used
// on logout so the refresh token can no longer be used.
public interface IRefreshTokenStore
{
    string Issue(string userId);
    RefreshRotation? Rotate(string oldToken);
    void Revoke(string token);
}

public sealed record RefreshRotation(string NewToken, string UserId);
