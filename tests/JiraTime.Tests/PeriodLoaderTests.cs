using System.Net;
using JiraTime.Domain;

namespace JiraTime.Tests;

public class PeriodLoaderTests : IDisposable
{
    private readonly TimeHarness _h = new(); // today is Tue 27 Oct 2026
    public void Dispose() => _h.Dispose();

    private PeriodSheet Sheet(DateOnly? day = null) => new(Period.For(day ?? _h.Today), () => _h.Today);

    [Fact]
    public async Task The_period_and_the_days_before_it_come_from_Jira()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-26T09:00:00.000+0100", 8 * 3600, "Login");
        _h.Jira.AddWorklog("ABC-2", "2026-10-14T09:00:00.000+0200", 3600); // previous period, within the days to copy from
        var sheet = Sheet();
        var (loader, statuses) = _h.PeriodLoading();

        var extras = await loader.LoadAsync(sheet, statuses);

        Assert.Null(extras.JiraError);
        Assert.True(sheet.HoursKnown);
        Assert.Equal(["ABC-1"], sheet.Entries.Select(e => e.IssueKey));
        Assert.Equal(["ABC-2"], sheet.Before.Select(e => e.IssueKey));
        Assert.Equal(16, sheet.Days.Count);
        Assert.Equal(["ABC-1", "ABC-2"], extras.Recent.Select(i => i.Key).Order());
        Assert.Equal(["Login"], extras.Comments["ABC-1"]);
    }

    [Fact]
    public async Task A_previous_period_with_missing_hours_is_reported_for_the_current_period_only()
    {
        var (loader, statuses) = _h.PeriodLoading();

        var current = await loader.LoadAsync(Sheet(), statuses);
        var older = await loader.LoadAsync(Sheet(new DateOnly(2026, 9, 20)), statuses);

        Assert.Equal(new DateOnly(2026, 10, 1), current.Previous?.Period.Start);
        Assert.True(current.Previous!.Missing > TimeSpan.Zero);
        Assert.Equal(0, current.Previous.NotInJira);
        Assert.Null(older.Previous);
    }

    [Fact]
    public async Task Without_Jira_hours_are_unknown_and_only_waiting_work_counts()
    {
        await _h.Queue.AddAsync(new TimeEntry { IssueKey = "ABC-3", Date = new DateOnly(2026, 10, 5), Duration = TimeSpan.FromHours(1), SyncPending = true });
        _h.Jira.FailWith = HttpStatusCode.ServiceUnavailable;
        var sheet = Sheet();
        var (loader, statuses) = _h.PeriodLoading();

        var extras = await loader.LoadAsync(sheet, statuses);

        Assert.NotNull(extras.JiraError);
        Assert.False(sheet.HoursKnown);
        Assert.Empty(sheet.Sprint);
        Assert.Empty(extras.Earlier);
        Assert.Equal((TimeSpan.Zero, 1), (extras.Previous!.Missing, extras.Previous.NotInJira));
    }

    [Fact]
    public async Task Statuses_of_logged_issues_are_looked_up()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-26T09:00:00.000+0100", 3600);
        _h.Jira.AddIssue("ABC-1", "Login", status: "Done", category: "done");
        var (loader, statuses) = _h.PeriodLoading();

        await loader.LoadAsync(Sheet(), statuses);

        Assert.True(statuses.Known["abc-1"].IsDone);
    }

    private int WorklogSearches => _h.Jira.Requests.Count(r => FakeJira.Jql(r).Contains("worklogAuthor"));
    private int WorklogReads => _h.Jira.Requests.Count(r => r.Path.Contains("/worklog"));

    [Fact]
    public async Task A_period_with_several_issues_reads_its_worklogs_with_one_search()
    {
        foreach (var key in new[] { "ABC-1", "ABC-2", "ABC-3", "ABC-4", "ABC-5" })
        {
            _h.Jira.AddWorklog(key, "2026-10-26T09:00:00.000+0100", 3600);
            _h.Jira.AddWorklog(key, "2026-09-10T09:00:00.000+0200", 1800, "Weeks ago"); // recent work, before the period
        }
        var (loader, statuses) = _h.PeriodLoading();

        var extras = await loader.LoadAsync(Sheet(), statuses);

        Assert.Equal(1, WorklogSearches);
        Assert.Equal(0, WorklogReads);
        Assert.Equal(4, _h.Jira.Requests.Count); // worklogs, sprint issues, period issues, statuses
        Assert.Equal(5, extras.Recent.Count);
        Assert.Equal(["Weeks ago"], extras.Comments["ABC-3"]);
    }

    [Fact]
    public async Task An_old_period_and_the_recent_days_are_read_apart()
    {
        var (loader, statuses) = _h.PeriodLoading();

        await loader.LoadAsync(Sheet(new DateOnly(2026, 3, 10)), statuses);

        Assert.Equal(2, WorklogSearches);
    }
}
