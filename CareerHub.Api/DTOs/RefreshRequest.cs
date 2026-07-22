namespace CareerHub.Api.DTOs;

// Assignment 2.4 — the refresh request. The Flutter client POSTs
// the current refresh token in the body (not in a header) so the
// endpoint can be treated identically to `/login` — no
// Authorization header, no interceptor involvement.
public record RefreshRequest(string RefreshToken);
