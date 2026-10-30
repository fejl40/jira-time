using System.Collections.Concurrent;

namespace JiraTime.Services;

/// <summary>
/// A thread-safe cache whose values expire <paramref name="lifetime"/> after they were stored.
/// Expired values are dropped whenever a value is stored, so a long-lived cache doesn't keep growing.
/// </summary>
public sealed class ExpiringCache<T>(TimeProvider clock, TimeSpan lifetime, IEqualityComparer<string>? keys = null)
{
    private readonly ConcurrentDictionary<string, (DateTimeOffset At, T Value)> _items = new(keys);

    // Counts Clear calls, so a value that was loading while the cache was cleared isn't stored afterwards.
    private long _generation;

    /// <summary>The value stored under <paramref name="key"/>, while it hasn't expired.</summary>
    public bool TryGet(string key, out T value)
    {
        if (_items.TryGetValue(key, out var hit) && IsLive(hit.At))
        {
            value = hit.Value;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>The values that haven't expired.</summary>
    public IReadOnlyList<T> Values
    {
        get
        {
            var live = _items.Values.Where(item => IsLive(item.At));
            return live.Select(item => item.Value).ToList();
        }
    }

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/> and drops the values that have expired.</summary>
    public void Set(string key, T value)
    {
        _items[key] = (clock.GetUtcNow(), value);
        RemoveExpired();
    }

    /// <summary>Forgets every value, so the next read loads again.</summary>
    public void Clear()
    {
        Interlocked.Increment(ref _generation);
        _items.Clear();
    }

    /// <summary>The cached value, or a new one from <paramref name="load"/> (always, with <paramref name="refresh"/>).</summary>
    public async Task<T> GetOrLoadAsync(string key, Func<Task<T>> load, bool refresh = false)
    {
        if (!refresh && TryGet(key, out var cached)) return cached;
        var generation = Interlocked.Read(ref _generation);
        var value = await load();
        if (Interlocked.Read(ref _generation) == generation) Set(key, value);
        return value;
    }

    private bool IsLive(DateTimeOffset storedAt) => clock.GetUtcNow() - storedAt < lifetime;

    private void RemoveExpired()
    {
        foreach (var item in _items)
        {
            if (!IsLive(item.Value.At)) _items.TryRemove(item);
        }
    }
}
