using System.Collections.Concurrent;
using CareerHub.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Services;

// Assignment 2.4 Stretch C — thread-safe in-memory store of
// (userId -> jobIds). Validates that the jobId corresponds to a
// real listing by querying the DbContext; unknown ids yield
// `SaveResult.NotFound` so the client can reconcile.
public sealed class InMemorySavedJobsStore : ISavedJobsStore
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _saved = new(StringComparer.Ordinal);
    private readonly IServiceScopeFactory _scopeFactory;

    public InMemorySavedJobsStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<SaveResult> SaveAsync(string userId, string jobId, CancellationToken ct)
    {
        // Validate the job exists. Uses a per-call scope so this
        // singleton doesn't capture a scoped DbContext.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareerHubDbContext>();

        if (!Guid.TryParse(jobId, out var guid))
        {
            return SaveResult.NotFound;
        }

        var exists = await db.JobListings
            .AsNoTracking()
            .AnyAsync(j => j.Id == guid, ct);
        if (!exists)
        {
            return SaveResult.NotFound;
        }

        var set = _saved.GetOrAdd(userId, _ => new HashSet<string>(StringComparer.Ordinal));
        lock (set)
        {
            set.Add(jobId);
        }
        return SaveResult.Saved;
    }

    public IReadOnlyCollection<string> List(string userId)
    {
        if (!_saved.TryGetValue(userId, out var set))
        {
            return Array.Empty<string>();
        }
        lock (set)
        {
            return set.ToArray();
        }
    }

    public void Remove(string userId, string jobId)
    {
        if (!_saved.TryGetValue(userId, out var set))
        {
            return;
        }
        lock (set)
        {
            set.Remove(jobId);
        }
    }
}
