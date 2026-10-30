using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>Reads that don't depend on each other go to Jira together, at most four at once; writes go one at a time.</summary>
public class ParallelReadsTests : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);
    private readonly TimeHarness _h = new(); // today is Tue 27 Oct 2026

    public ParallelReadsTests() => _h.Jira.Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Dispose() => _h.Dispose();

    private async Task ArrivedAsync(int requests)
    {
        for (var i = 0; i < requests; i++)
            Assert.True(await _h.Jira.Arrived.WaitAsync(Patience), $"Only {i} of {requests} requests reached Jira.");
    }

    [Fact]
    public async Task Opening_a_period_reads_worklogs_sprint_issues_and_period_issues_at_the_same_time()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-26T09:00:00.000+0100", 3600);
        var (loader, statuses) = _h.PeriodLoading();
        var sheet = new PeriodSheet(Period.For(_h.Today), () => _h.Today);

        Task loading;
        using (JiraAction.Start("Open period"))
            loading = loader.LoadAsync(sheet, statuses);
        await ArrivedAsync(3); // all three are out before Jira answered any of them
        _h.Jira.Hold!.SetResult();
        await loading;

        Assert.Equal(3, _h.Jira.MostInFlight);
        var log = await _h.RequestLog.ReadDayAsync(TestUsers.Carl.Id, _h.Today);
        Assert.Single(log.Select(e => e.ActionId).Distinct()); // still grouped under the action
        Assert.All(log, e => Assert.Equal("Open period", e.Action));
    }

    [Fact]
    public async Task Issues_read_on_their_own_go_out_together_at_most_four_at_once()
    {
        _h.Jira.EmbeddedWorklogLimit = 0; // the search sends no worklogs, so each issue is read on its own
        for (var i = 1; i <= 6; i++) _h.Jira.AddWorklog($"ABC-{i}", "2026-10-26T09:00:00.000+0100", 3600);
        _h.Jira.HoldWhen = r => r.Path.Contains("/worklog");

        var reading = _h.Log.GetRangeAsync(_h.Today.AddDays(-7), _h.Today);
        await ArrivedAsync(1 + JiraResilience.MaxInFlight); // the search, then four issues
        Assert.Equal(JiraResilience.MaxInFlight, _h.Jira.Requests.Count(r => r.Path.Contains("/worklog")));
        _h.Jira.Hold!.SetResult();
        var time = await reading;

        Assert.Equal(6, time.Entries.Count);
        Assert.Equal(6, _h.Jira.Requests.Count(r => r.Path.Contains("/worklog")));
        Assert.Equal(JiraResilience.MaxInFlight, _h.Jira.MostInFlight);
    }

    [Fact]
    public async Task Saving_a_day_writes_one_worklog_at_a_time()
    {
        _h.Jira.HoldWhen = r => r.Method == HttpMethod.Post;
        var saved = await _h.DayAsync(_h.Today);
        while (_h.Jira.Arrived.Wait(0)) { } // forget the read

        var saving = _h.SaveDayAsync(_h.Today, [TimeHarness.Row("ABC-1", 1), TimeHarness.Row("ABC-2", 2), TimeHarness.Row("ABC-3", 3)], saved);
        await ArrivedAsync(1);
        for (var i = 0; i < 20; i++) await Task.Yield();
        Assert.Single(_h.Jira.Requests, r => r.Method == HttpMethod.Post);
        _h.Jira.Hold!.SetResult();
        await saving;

        Assert.Equal(3, _h.Jira.Requests.Count(r => r.Method == HttpMethod.Post));
        Assert.Equal(1, _h.Jira.MostInFlight);
    }
}
