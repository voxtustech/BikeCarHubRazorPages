using System.Collections.Concurrent;

namespace BikeCarHub.Services;

/// <summary>
/// Locks an email address for a while after repeated wrong passwords. Kept in memory so that signing in
/// still never writes to the database; the per-IP rate limit on the form is a separate, outer limit.
/// </summary>
public interface ISignInThrottle
{
    bool IsLocked(string email);
    void RecordFailure(string email);
    void Reset(string email);
}

public sealed class SignInThrottle(TimeProvider clock) : ISignInThrottle
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private sealed record Entry(int Failures, DateTimeOffset WindowStart);
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public SignInThrottle() : this(TimeProvider.System) { }

    public bool IsLocked(string email)
    {
        var key = Key(email);
        if (!_entries.TryGetValue(key, out var entry)) return false;
        if (Expired(entry))
        {
            _entries.TryRemove(key, out _);
            return false;
        }
        return entry.Failures >= MaxFailures;
    }

    public void RecordFailure(string email)
    {
        _entries.AddOrUpdate(Key(email),
            _ => new Entry(1, clock.GetUtcNow()),
            (_, e) => Expired(e) ? new Entry(1, clock.GetUtcNow()) : e with { Failures = e.Failures + 1 });
        if (_entries.Count > 10_000) Prune();
    }

    public void Reset(string email) => _entries.TryRemove(Key(email), out _);

    private bool Expired(Entry e) => clock.GetUtcNow() - e.WindowStart >= Window;

    private void Prune()
    {
        foreach (var (key, entry) in _entries)
            if (Expired(entry)) _entries.TryRemove(key, out _);
    }

    private static string Key(string email) => email.Trim().ToUpperInvariant();
}
