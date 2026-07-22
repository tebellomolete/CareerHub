namespace CareerHub.Api.DTOs;

// Assignment 2.4 — the login request the Flutter client sends.
// Field renamed from `Username` → `Email` in Assignment 2.4 because
// the mobile app displays an email keyboard and stores `email` as a
// first-class field on its `User` domain model. The two hardcoded
// identities are now email-shaped strings (see
// `InMemoryUserAccountStore`); no DB change was required.
public record LoginRequest(string Email, string Password);
