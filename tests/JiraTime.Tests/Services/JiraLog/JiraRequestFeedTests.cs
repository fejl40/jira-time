using JiraTime.Services.JiraLog;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services.JiraLog;

/// <summary>The live feed of Jira requests: per user, and never held up by its subscribers.</summary>
public class JiraRequestFeedTests : IDisposable
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private readonly TimeHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static async Task WaitUntil(Func<bool> done)
    {
        var until = DateTime.UtcNow + Wait;
        while (!done() && DateTime.UtcNow < until) await Task.Delay(10);
        Assert.True(done(), "Timed out waiting for the feed.");
    }

    private IDisposable Collect(string owner, List<JiraRequestEvent> events) =>
        _h.Feed.Subscribe(owner, e =>
        {
            lock (events) events.Add(e);
            return Task.CompletedTask;
        });

    [Fact]
    public async Task Each_request_is_told_as_started_then_finished()
    {
        var events = new List<JiraRequestEvent>();
        using var _ = Collect(TestUsers.Carl.Id, events);

        await _h.Log.GetRangeAsync(_h.Today, _h.Today);

        await WaitUntil(() => events.Count == 2);
        Assert.Equal([JiraRequestStage.Started, JiraRequestStage.Finished], events.Select(e => e.Stage));
        Assert.Equal(events[0].Entry.Id, events[1].Entry.Id);
        Assert.True(events[0].Entry.IsPending);
        Assert.Equal(200, events[1].Entry.Status);
        Assert.NotNull(events[1].Entry.DurationMs);
    }

    [Fact]
    public async Task Other_users_never_hear_of_a_request()
    {
        var carls = new List<JiraRequestEvent>();
        var annas = new List<JiraRequestEvent>();
        using var c = Collect(TestUsers.Carl.Id, carls);
        using var a = Collect(TestUsers.Anna.Id, annas);

        await _h.Log.GetRangeAsync(_h.Today, _h.Today);

        await WaitUntil(() => carls.Count == 2);
        await Task.Delay(50);
        Assert.Empty(annas);
    }

    [Fact]
    public async Task A_failing_or_stuck_subscriber_never_holds_up_the_request_or_other_subscribers()
    {
        var stuck = new TaskCompletionSource();
        using var s = _h.Feed.Subscribe(TestUsers.Carl.Id, _ => stuck.Task);
        using var f = _h.Feed.Subscribe(TestUsers.Carl.Id, _ => throw new InvalidOperationException("boom"));
        var events = new List<JiraRequestEvent>();
        using var c = Collect(TestUsers.Carl.Id, events);

        var load = _h.Log.GetRangeAsync(_h.Today, _h.Today);

        Assert.Same(load, await Task.WhenAny(load, Task.Delay(Wait)));
        await load;
        await WaitUntil(() => events.Count == 2);
        stuck.SetResult();
    }

    [Fact]
    public async Task A_disposed_subscription_hears_nothing_more()
    {
        var events = new List<JiraRequestEvent>();
        Collect(TestUsers.Carl.Id, events).Dispose();

        await _h.Log.GetRangeAsync(_h.Today, _h.Today);

        await Task.Delay(50);
        Assert.Empty(events);
    }
}
