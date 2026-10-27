using System.Collections.Concurrent;

namespace JiraTime.Services;

/// <summary>A thread-safe cache whose values expire <paramref name="lifetime"/> after they were stored.</summary>
public sealed class ExpiringCache<T>(TimeProvider clock, TimeSpan lifetime)
{
    private readonly ConcurrentDictionary<string, (DateTimeOffset At, T Value)> _items = new();

    /// <summary>The value stored under <paramref name="key"/>, while it hasn't expired.</summary>
    public bool TryGet(string key, out T value)
    {
        if (_items.TryGetValue(key, out var hit) && clock.GetUtcNow() - hit.At < lifetime)
        {
            value = hit.Value;
            return true;
        }
        value = default!;
        return false;
    }

    public void Set(string key, T value) => _items[key] = (clock.GetUtcNow(), value);

    /// <summary>The cached value, or a new one from <paramref name="load"/> (always, with <paramref name="refresh"/>).</summary>
    public async Task<T> GetOrLoadAsync(string key, Func<Task<T>> load, bool refresh = false)
    {
        if (!refresh && TryGet(key, out var cached)) return cached;
        var value = await load();
        Set(key, value);
        return value;
    }
}
