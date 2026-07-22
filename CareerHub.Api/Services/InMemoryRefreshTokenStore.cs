using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace CareerHub.Api.Services;

// Assignment 2.4 — a thread-safe in-memory refresh-token store.
// Fine for two hardcoded users and for the assignment demo; a
// production build would use Redis / a table with a unique index.
//
// Every entry expires 30 days after issuance. Expired entries are
// removed on read (lazy sweep) rather than on a background timer
// — no cost when nobody signs in, no scheduler to configure.
public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);

    public string Issue(string userId)
    {
        var token = GenerateOpaqueToken();
        _store[token] = new Entry(userId, DateTime.UtcNow.Add(Lifetime));
        return token;
    }

    public RefreshRotation? Rotate(string oldToken)
    {
        // TryRemove is atomic — the first caller wins the race and
        // every subsequent caller with the same token sees the
        // removal and returns null. This is the mechanism that
        // makes three concurrent refresh calls sharing one token
        // rotate to exactly one new token, with the other two
        // failing loudly.
        if (!_store.TryRemove(oldToken, out var entry))
        {
            return null;
        }

        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        var newToken = Issue(entry.UserId);
        return new RefreshRotation(newToken, entry.UserId);
    }

    public void Revoke(string token)
    {
        _store.TryRemove(token, out _);
    }

    private static string GenerateOpaqueToken()
    {
        // 32 bytes = 256 bits of entropy. Base64URL for a
        // URL-safe, no-padding string — convenient if a future
        // caller ever puts it in a query string, and avoids the
        // `+ / =` characters that need percent-encoding.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed record Entry(string UserId, DateTime ExpiresAt);
}
