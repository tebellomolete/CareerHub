using CareerHub.Api.Data;

namespace CareerHub.Api.Services;

// Assignment 2.4 Stretch C — the very small saved-jobs store.
//
// `Save(userId, jobId)` — records the bookmark. Idempotent: saving
//   an already-saved job is a no-op that returns Success.
//   Returns SaveResult.NotFound if the job listing doesn't exist
//   (the 404 case the stretch spec calls out).
// `List(userId)` — returns the set of jobIds the user has saved.
// `Remove(userId, jobId)` — unbookmarks. Silent no-op if the
//   bookmark doesn't exist (the client is source of truth for
//   what it thinks is saved; the server accepts idempotent removes).
public interface ISavedJobsStore
{
    Task<SaveResult> SaveAsync(string userId, string jobId, CancellationToken ct);
    IReadOnlyCollection<string> List(string userId);
    void Remove(string userId, string jobId);
}

public enum SaveResult
{
    Saved,
    NotFound,
}
