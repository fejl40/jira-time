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
        _h.Jira.Gets["/rest/api/2/search"] = _ => new
        {
            startAt = 0, total = 1,
            issues = new[] { new { key = "ABC-1", fields = new { summary = "Login", issuetype = new { name = "Story" }, status = new { name = "Done", statusCategory = new { key = "done" } } } } },
        };
        var (loader, statuses) = _h.PeriodLoading();

        await loader.LoadAsync(Sheet(), statuses);

        Assert.True(statuses.Known["abc-1"].IsDone);
    }
}
