namespace CareerHub.Api.DTOs;

// Assignment 2.4 Stretch C — the request body for POST /api/v1/saved.
// The client-side `SavedJobsRepository` uses this endpoint from both
// the online path (immediate save) and the reconciliation path
// (after a connectivity restore drains the pending-sync queue).
public record SaveJobRequest(string JobId);
