using JiraTime.Services;

namespace JiraTime.Tests;

public class ExpiringCacheTests
{
    [Fact]
    public async Task Values_are_loaded_once_until_they_expire_or_a_refresh_is_asked()
    {
        var clock = new TestClock();
        var cache = new ExpiringCache<int>(clock, TimeSpan.FromMinutes(5));
        var loads = 0;
        Task<int> Load() => Task.FromResult(++loads);

        Assert.Equal(1, await cache.GetOrLoadAsync("k", Load));
        clock.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(1, await cache.GetOrLoadAsync("k", Load));
        Assert.Equal(2, await cache.GetOrLoadAsync("k", Load, refresh: true));
        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.False(cache.TryGet("k", out _));
        Assert.Equal(3, await cache.GetOrLoadAsync("k", Load));
    }
}
