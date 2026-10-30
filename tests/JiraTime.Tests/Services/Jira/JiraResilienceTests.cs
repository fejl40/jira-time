using System.Net;
using JiraTime.Domain;
using JiraTime.Domain.JiraLog;
using JiraTime.Services.Jira;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services.Jira;

/// <summary>Jira's rate limits: a 429 pauses every request to that host, requests are retried after it, and a queue caps how many are out.</summary>
public class JiraResilienceTests : IDisposable
{
    private readonly TimeHarness _h = new();
    private readonly DateTimeOffset _start;

    public JiraResilienceTests() => _start = _h.Clock.Now;

    public void Dispose() => _h.Dispose();

    private static JiraConnection Anna => TimeHarness.Server with { Token = "annas-token", Owner = TestUsers.Anna.Id };

    private static readonly JiraConnection OtherHost = new("https://other-jira.example.com", JiraAuthType.Pat, "token", null) { Owner = TestUsers.Carl.Id };

    private Task<IReadOnlyList<JiraLogEntry>> LogAsync(string owner) => _h.RequestLog.ReadDayAsync(owner, _h.Today);

    [Fact]
    public async Task A_429_is_sent_again_after_Retry_After_and_both_attempts_are_logged()
    {
        _h.Jira.TooManyRequests = 1;
        _h.Jira.RetryAfter = "2";

        await _h.MyselfAsync();

        Assert.Equal(2, _h.Jira.Requests.Count);
        Assert.InRange(_h.Clock.Now - _start, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
        var log = await LogAsync(TestUsers.Carl.Id);
        Assert.Equal([429, 200], log.Select(e => e.Status));
        Assert.Equal("Jira asked us to slow down: requests to Jira pause for 2 s.", log[0].Error);
        Assert.Null(log[1].Error);
    }

    [Fact]
    public async Task Retry_After_as_a_date_is_waited_out_too()
    {
        _h.Jira.TooManyRequests = 1;
        _h.Jira.RetryAfter = (_start + TimeSpan.FromSeconds(4)).ToString("r");

        await _h.MyselfAsync();

        Assert.InRange(_h.Clock.Now - _start, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task A_write_Jira_refused_with_429_is_sent_again_and_logged_once_in_Jira()
    {
        var saved = await _h.DayAsync(_h.Today);
        _h.Jira.TooManyRequests = 1;
        _h.Jira.RetryAfter = "1";

        var result = await _h.SaveDayAsync(_h.Today, [TestEntries.Row("ABC-1", 2)], saved);

        Assert.Equal(0, result.SyncFailures);
        Assert.Equal(2, _h.Jira.Writes.Count());
        Assert.Single(_h.Jira.Worklogs["ABC-1"]);
    }

    [Fact]
    public async Task A_write_that_fails_with_a_server_error_is_not_sent_again()
    {
        _h.Jira.FailWhen = uri => uri.AbsolutePath.EndsWith("/worklog") ? HttpStatusCode.ServiceUnavailable : null;

        var result = await _h.SaveDayAsync(_h.Today, [TestEntries.Row("ABC-1", 2)]);

        Assert.Equal(1, result.SyncFailures);
        Assert.Single(_h.Jira.Writes);
        Assert.Single(await _h.QueuedAsync());
    }

    [Fact]
    public async Task A_429_that_keeps_coming_fails_after_a_few_tries()
    {
        _h.Jira.TooManyRequests = 100;
        _h.Jira.RetryAfter = "1";

        var ex = await Assert.ThrowsAsync<JiraException>(() => _h.MyselfAsync());

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.Status);
        Assert.Equal(JiraResilience.SlowDownMessage, ex.Message);
        Assert.InRange(_h.Jira.Requests.Count, 2, 1 + JiraResilience.MaxRetries);
        Assert.True(_h.Clock.Now - _start < TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task A_longer_pause_than_the_cap_fails_at_once()
    {
        await _h.MyselfAsync();
        _h.Jira.TooManyRequests = 1;
        _h.Jira.RetryAfter = "60";

        await Assert.ThrowsAsync<JiraException>(() => _h.MyselfAsync());

        Assert.Equal(2, _h.Jira.Requests.Count);
        Assert.Equal(_start, _h.Clock.Now);
    }

    [Fact]
    public async Task A_429_for_one_user_pauses_another_users_requests_to_the_same_host()
    {
        await _h.MyselfAsync();
        _h.Jira.TooManyRequests = 1;
        _h.Jira.RetryAfter = "30"; // over the cap, so Carl's request gives up and the pause stays open
        await Assert.ThrowsAsync<JiraException>(() => _h.MyselfAsync());

        // While the host is paused, Anna's request doesn't reach Jira.
        await Assert.ThrowsAsync<JiraException>(() => _h.MyselfAsync(Anna));
        Assert.Equal(2, _h.Jira.Requests.Count);

        // Near the end of the pause it waits for the rest, then goes.
        _h.Clock.Advance(TimeSpan.FromSeconds(29));
        await _h.MyselfAsync(Anna);
        var annas = Assert.Single(await LogAsync(TestUsers.Anna.Id));
        Assert.True(annas.Time >= _start + TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task A_429_from_one_Jira_host_does_not_pause_another()
    {
        await _h.MyselfAsync();
        _h.Jira.TooManyRequests = 1;
        _h.Jira.RetryAfter = "30";
        await Assert.ThrowsAsync<JiraException>(() => _h.MyselfAsync());

        await _h.MyselfAsync(OtherHost);

        Assert.Equal(3, _h.Jira.Requests.Count);
        Assert.Equal(_start, _h.Clock.Now);
    }

    [Fact]
    public async Task At_most_four_requests_are_out_at_once_and_the_rest_wait_in_turn()
    {
        _h.Jira.Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var requests = Enumerable.Range(0, 7).Select(_ => _h.MyselfAsync()).ToList();
        for (var i = 0; i < JiraResilience.MaxInFlight; i++)
            Assert.True(await _h.Jira.Arrived.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(JiraResilience.MaxInFlight, _h.Jira.Requests.Count);
        _h.Jira.Hold.SetResult();
        await Task.WhenAll(requests);

        Assert.Equal(7, _h.Jira.Requests.Count);
        Assert.Equal(JiraResilience.MaxInFlight, _h.Jira.MostInFlight);
    }
}
