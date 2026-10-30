using JiraTime.Services;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services;

public class ExpiringCacheTests
{
    private readonly TestClock _clock = new();

    [Fact]
    public async Task Values_are_loaded_once_until_they_expire_or_a_refresh_is_asked()
    {
        var cache = new ExpiringCache<int>(_clock, TimeSpan.FromMinutes(5));
        var loads = 0;
        Task<int> Load() => Task.FromResult(++loads);

        Assert.Equal(1, await cache.GetOrLoadAsync("k", Load));
        _clock.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(1, await cache.GetOrLoadAsync("k", Load));
        Assert.Equal(2, await cache.GetOrLoadAsync("k", Load, refresh: true));
        _clock.Advance(TimeSpan.FromMinutes(5));
        Assert.False(cache.TryGet("k", out _));
        Assert.Equal(3, await cache.GetOrLoadAsync("k", Load));
    }

    [Fact]
    public void Clearing_forgets_every_value()
    {
        var cache = new ExpiringCache<int>(_clock, TimeSpan.FromMinutes(5));
        cache.Set("a", 1);
        cache.Set("b", 2);

        cache.Clear();

        Assert.False(cache.TryGet("a", out _));
        Assert.Empty(cache.Values);
    }

    [Fact]
    public async Task A_value_that_was_loading_while_the_cache_was_cleared_is_not_kept()
    {
        var cache = new ExpiringCache<int>(_clock, TimeSpan.FromMinutes(5));
        var loading = new TaskCompletionSource<int>();

        var load = cache.GetOrLoadAsync("k", () => loading.Task);
        cache.Clear();
        loading.SetResult(1);

        Assert.Equal(1, await load);
        Assert.False(cache.TryGet("k", out _));
    }

    [Fact]
    public void Values_holds_only_the_values_that_have_not_expired()
    {
        var cache = new ExpiringCache<string>(_clock, TimeSpan.FromMinutes(5));
        cache.Set("old", "old");
        _clock.Advance(TimeSpan.FromMinutes(3));
        cache.Set("new", "new");
        _clock.Advance(TimeSpan.FromMinutes(3));

        Assert.Equal(["new"], cache.Values);
    }

    [Fact]
    public void Storing_a_value_removes_the_expired_ones_from_memory()
    {
        var cache = new ExpiringCache<string>(_clock, TimeSpan.FromMinutes(5));
        var start = _clock.Now;
        cache.Set("old", "old");
        _clock.Advance(TimeSpan.FromMinutes(6));

        cache.Set("new", "new");
        _clock.Now = start; // back to when "old" was stored: it would count as live again had it stayed in memory

        Assert.False(cache.TryGet("old", out _));
    }

    [Fact]
    public void Keys_can_ignore_case()
    {
        var cache = new ExpiringCache<int>(_clock, TimeSpan.FromMinutes(5), StringComparer.OrdinalIgnoreCase);
        cache.Set("abc-1", 1);
        cache.Set("ABC-1", 2);

        Assert.True(cache.TryGet("Abc-1", out var value));
        Assert.Equal(2, value);
        Assert.Single(cache.Values);
    }
}
