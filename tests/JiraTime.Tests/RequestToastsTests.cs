using JiraTime.Domain;

namespace JiraTime.Tests;

/// <summary>The toasts for live Jira requests: order, how long they stay, pausing and bursts.</summary>
public class RequestToastsTests
{
    private readonly TestClock _clock = new();
    private readonly RequestToasts _toasts;

    public RequestToastsTests() => _toasts = new RequestToasts(_clock);

    private static JiraLogEntry Started(string id) => new() { Id = id, Method = "GET" };
    private static JiraLogEntry Finished(string id, int status = 200) => Started(id) with { Status = status, DurationMs = 120 };

    private void After(TimeSpan time)
    {
        _clock.Advance(time);
        _toasts.Tick();
    }

    [Fact]
    public void Newest_is_last_and_a_finish_updates_its_toast_in_place()
    {
        _toasts.Update(Started("a"));
        _toasts.Update(Started("b"));
        _toasts.Update(Finished("a"));

        Assert.Equal(["a", "b"], _toasts.Shown.Select(t => t.Entry.Id));
        Assert.False(_toasts.Shown[0].Entry.IsPending);
        Assert.True(_toasts.Shown[1].Entry.IsPending);
    }

    [Fact]
    public void A_finished_request_fades_after_five_seconds_and_is_then_gone()
    {
        _toasts.Update(Started("a"));
        _toasts.Update(Finished("a"));

        After(TimeSpan.FromSeconds(4.9));
        Assert.False(_toasts.Shown[0].Leaving);
        After(TimeSpan.FromSeconds(0.2));
        Assert.True(_toasts.Shown[0].Leaving);
        After(RequestToasts.Fade);
        Assert.False(_toasts.Any);
    }

    [Fact]
    public void A_request_still_out_stays_until_it_finishes()
    {
        _toasts.Update(Started("a"));

        After(TimeSpan.FromMinutes(1));

        Assert.Single(_toasts.Shown);
    }

    [Fact]
    public void A_failed_request_stays_fifteen_seconds_or_until_closed()
    {
        _toasts.Update(Finished("a", 500));
        _toasts.Update(Finished("b", 500));

        After(TimeSpan.FromSeconds(14));
        Assert.Equal(2, _toasts.Shown.Count(t => !t.Leaving));
        _toasts.Close("a");
        Assert.Equal(["b"], _toasts.Shown.Select(t => t.Entry.Id));
        After(TimeSpan.FromSeconds(1.1));
        Assert.True(_toasts.Shown[0].Leaving);
    }

    [Fact]
    public void Hovering_pauses_the_fade_and_leaving_resumes_it()
    {
        _toasts.Update(Finished("a"));
        After(TimeSpan.FromSeconds(3));

        _toasts.Pause();
        After(TimeSpan.FromMinutes(1));
        _toasts.Update(Finished("b"));
        After(TimeSpan.FromMinutes(1));
        Assert.Equal(2, _toasts.Shown.Count(t => !t.Leaving));

        _toasts.Resume();
        After(TimeSpan.FromSeconds(2.1));
        Assert.True(_toasts.Shown[0].Leaving);
        Assert.False(_toasts.Shown[1].Leaving);
    }

    [Fact]
    public void A_burst_shows_the_five_newest_and_counts_the_rest()
    {
        for (var i = 0; i < 8; i++) _toasts.Update(Started($"r{i}"));

        Assert.Equal(["r3", "r4", "r5", "r6", "r7"], _toasts.Shown.Select(t => t.Entry.Id));
        Assert.Equal(3, _toasts.Hidden);
    }

    [Fact]
    public void A_start_arriving_after_its_finish_is_ignored()
    {
        _toasts.Update(Finished("a"));
        _toasts.Update(Started("a"));

        Assert.False(Assert.Single(_toasts.Shown).Entry.IsPending);
    }
}
