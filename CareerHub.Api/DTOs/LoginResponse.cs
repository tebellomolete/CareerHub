namespace CareerHub.Api.DTOs;

// Assignment 2.4 — the login response the Flutter client consumes.
// Previously a single `Token` field; now a token PAIR because the
// mobile app implements the standard access-token + rotating
// refresh-token pattern documented in Question 3 of README 2.4.
//
// `AccessToken` — short-lived (5 min in this build), attached to
// every authenticated request via `AuthInterceptor`.
// `RefreshToken` — long-lived (30 days), used only against
// `/api/v1/auth/refresh` to obtain a new access token when the
// current one expires.
public record LoginResponse(string AccessToken, string RefreshToken);
