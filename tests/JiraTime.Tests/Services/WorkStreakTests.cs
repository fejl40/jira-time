using System.Net;
using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services;

public class WorkStreakTests : IDisposable
{
    private readonly TimeHarness _h = new(); // today is Tue 27 Oct 2026
    public void Dispose() => _h.Dispose();

    private WorkStreak Streak => _h.Get<WorkStreak>();

    private async Task OpenPeriodAsync(DateOnly? day = null)
    {
        var (loader, statuses) = _h.PeriodLoading();
        await loader.LoadAsync(new PeriodSheet(Period.For(day ?? _h.Today), () => _h.Today), statuses);
    }

    private void LogFullDays()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-26T09:00:00.000+0100", 8 * 3600); // Mon
        _h.Jira.AddWorklog("ABC-1", "2026-10-23T09:00:00.000+0200", 8 * 3600); // Fri
        _h.Jira.AddWorklog("ABC-2", "2026-10-22T09:00:00.000+0200", 8 * 3600); // Thu
        _h.Jira.AddWorklog("ABC-2", "2026-10-21T09:00:00.000+0200", 4 * 3600); // Wed, short
    }

    [Fact]
    public async Task Opening_a_period_counts_the_streak_from_the_work_it_reads_anyway()
    {
        LogFullDays();

        await OpenPeriodAsync();

        Assert.Equal(new Streak(3, false), Streak.Current);
        Assert.Equal(4, _h.Jira.Requests.Count); // worklogs, sprint issues, period issues, statuses: nothing extra
    }

    [Fact]
    public async Task An_old_period_counts_from_the_recent_days_read_with_it()
    {
        LogFullDays();

        await OpenPeriodAsync(new DateOnly(2026, 3, 10));

        Assert.Equal(3, Streak.Current?.Days);
        Assert.Equal(2, _h.Jira.WorklogSearches.Count());
    }

    [Fact]
    public async Task There_is_no_streak_while_Jira_cant_be_read()
    {
        LogFullDays();
        await OpenPeriodAsync();
        var changes = 0;
        Streak.Changed += () => changes++;
        _h.Jira.FailWith = HttpStatusCode.ServiceUnavailable;
        _h.Log.Refresh();

        await OpenPeriodAsync();

        Assert.Null(Streak.Current);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task Another_user_in_the_tab_doesnt_see_the_streak()
    {
        LogFullDays();
        await OpenPeriodAsync();

        TestUsers.SignIn(_h.User, TestUsers.Anna, _h.Connection);

        Assert.Null(Streak.Current);
    }
}
