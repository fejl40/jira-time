using JiraTime.Domain;
using JiraTime.Services.Jira;
using JiraTime.Services.JiraLog;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services.Worklogs;

/// <summary>
/// Reads that don't depend on each other go to Jira together, at most JiraResilience.MaxInFlight at once; writes go one at a time.
/// Named after that behaviour, as it spans PeriodLoader, TimeLog, WorklogSync and the Jira client's limiter.
/// </summary>
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
    public async Task Opening_an_old_period_reads_it_and_the_recent_days_at_the_same_time()
    {
        var (loader, statuses) = _h.PeriodLoading();
        var sheet = new PeriodSheet(Period.For(new DateOnly(2026, 3, 2)), () => _h.Today);

        var loading = loader.LoadAsync(sheet, statuses);
        await ArrivedAsync(4); // the period's worklogs, the recent days, sprint issues and period issues
        _h.Jira.Hold!.SetResult();
        await loading;

        Assert.Equal(2, _h.Jira.WorklogSearches.Count());
        Assert.Equal(4, _h.Jira.MostInFlight);
    }

    [Fact]
    public async Task Issues_read_on_their_own_go_out_together_up_to_the_limit()
    {
        _h.Jira.EmbeddedWorklogLimit = 0; // the search sends no worklogs, so each issue is read on its own
        var issues = JiraResilience.MaxInFlight + 2;
        for (var i = 1; i <= issues; i++) _h.Jira.AddWorklog($"ABC-{i}", "2026-10-26T09:00:00.000+0100", 3600);
        _h.Jira.HoldWhen = r => r.Path.Contains("/worklog");

        var reading = _h.Log.GetRangeAsync(_h.Today.AddDays(-7), _h.Today);
        await ArrivedAsync(1 + JiraResilience.MaxInFlight); // the search, then as many issues as the limit allows
        Assert.Equal(JiraResilience.MaxInFlight, _h.Jira.WorklogReads.Count());
        _h.Jira.Hold!.SetResult();
        var time = await reading;

        Assert.Equal(issues, time.Entries.Count);
        Assert.Equal(issues, _h.Jira.WorklogReads.Count());
        Assert.Equal(JiraResilience.MaxInFlight, _h.Jira.MostInFlight);
    }

    [Fact]
    public async Task Issues_just_saved_on_are_read_together_with_the_worklog_search()
    {
        _h.Jira.Hold = null;
        await _h.SaveDayAsync(_h.Today, [TestEntries.Row("NEW-1", 3)]);
        _h.Jira.NotIndexed.Add("NEW-1");
        _h.Log.Refresh();
        while (_h.Jira.Arrived.Wait(0)) { } // forget the save
        _h.Jira.Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var reading = _h.Log.GetRangeAsync(_h.Today.AddDays(-7), _h.Today);
        await ArrivedAsync(2); // both are out before Jira answered either
        _h.Jira.Hold.SetResult();
        var time = await reading;

        Assert.Equal("NEW-1", Assert.Single(time.Entries).IssueKey);
    }

    [Fact]
    public async Task Saving_a_day_writes_one_worklog_at_a_time()
    {
        _h.Jira.HoldWhen = r => r.Method == HttpMethod.Post;
        var saved = await _h.DayAsync(_h.Today);
        while (_h.Jira.Arrived.Wait(0)) { } // forget the read

        var saving = _h.SaveDayAsync(_h.Today, [TestEntries.Row("ABC-1", 1), TestEntries.Row("ABC-2", 2), TestEntries.Row("ABC-3", 3)], saved);
        await ArrivedAsync(1);
        for (var i = 0; i < 20; i++) await Task.Yield();
        Assert.Single(_h.Jira.Writes);
        _h.Jira.Hold!.SetResult();
        await saving;

        Assert.Equal(3, _h.Jira.Writes.Count());
        Assert.Equal(1, _h.Jira.MostInFlight);
    }
}
