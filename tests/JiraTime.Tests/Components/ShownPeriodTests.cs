using JiraTime.Components;
using JiraTime.Components.Sheet;
using JiraTime.Domain;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Components;

/// <summary>The timesheet's period and its work with Jira, without the page around it.</summary>
public class ShownPeriodTests : IDisposable
{
    private static readonly DateOnly Monday = new(2026, 10, 19), Tuesday = new(2026, 10, 20);
    /// <summary>A day still to come: showing it suggests nothing, so only the rows a test adds are unsaved.</summary>
    private static readonly DateOnly Thursday = new(2026, 10, 29);

    private readonly TimeHarness _h = new(); // today is Tue 27 Oct 2026
    public void Dispose() => _h.Dispose();

    private ShownPeriod NewShownPeriod()
    {
        var (loader, statuses) = _h.PeriodLoading();
        return new ShownPeriod(loader, statuses, _h.Sync, _h.Log, _h.Calendar, _h.User, _h.Clock);
    }

    private static TimesheetLink Link(DateOnly day) => new(Period.For(day), day);

    private static void Add(PeriodSheet sheet, DateOnly day, string key, string hours) =>
        sheet.Day(day).SetHours(sheet.Day(day).AddRow(new JiraIssue(key, $"Summary of {key}")), hours);

    [Fact]
    public async Task Save_all_selects_the_first_day_that_cant_be_saved_and_says_why()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Thursday));
        Add(shown.Sheet!, Monday, "ABC-1", "lots");
        Add(shown.Sheet!, Tuesday, "ABC-2", "8");
        var reloaded = false;

        var toFix = await shown.SaveDaysAsync(() => { reloaded = shown.Waits.Has(JiraWait.SaveAll); return Task.CompletedTask; });

        Assert.Equal(Monday, toFix);
        Assert.StartsWith("1 day not saved", shown.Notice);
        Assert.True(reloaded); // ran while the save was still running, before the period was read again
        Assert.Equal(8 * 3600, _h.JiraSeconds("ABC-2"));
        Assert.Equal([Tuesday], shown.SaveMotion.JustSaved);
        Assert.True(shown.SaveMotion.TakeFlyTicks());
        Assert.True(shown.Waits.CanStart);
    }

    [Fact]
    public async Task The_notice_stays_when_save_all_opens_the_day_to_fix_and_goes_when_the_user_moves_on()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Thursday));
        Add(shown.Sheet!, Monday, "ABC-1", "lots");

        var toFix = await shown.SaveDaysAsync(() => Task.CompletedTask);
        await shown.ShowAsync(Link(toFix!.Value)); // the page selects the day through the URL

        Assert.Equal(Monday, shown.Day);
        Assert.StartsWith("1 day not saved", shown.Notice);

        await shown.ShowAsync(Link(Tuesday));
        Assert.Null(shown.Notice);
        await shown.ShowAsync(Link(Monday));
        Assert.Null(shown.Notice);
    }

    [Fact]
    public async Task When_every_day_is_saved_nothing_is_selected_and_the_notice_is_gone()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Thursday));
        shown.Notice = "Old news";
        Add(shown.Sheet!, Tuesday, "ABC-2", "8");

        Assert.Null(await shown.SaveDaysAsync(() => Task.CompletedTask));
        Assert.Null(shown.Notice);
        Assert.Empty(shown.Sheet!.UnsavedDays);
    }

    [Fact]
    public async Task A_whole_day_off_marks_the_saved_rows_removed_after_reading_the_period_again()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 8 * 3600);
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Monday));

        await shown.MarkDayOffAsync(Monday, new TimeOffModal.TimeOff(DayOffReason.Vacation, null));

        var day = shown.Sheet!.Day(Monday);
        Assert.Equal(DayKind.DayOff, day.Info?.Kind);
        Assert.All(day.Rows, row => Assert.True(row.Removed));
        Assert.Single(day.Rows);

        await shown.UnmarkDayOffAsync(Monday);

        Assert.Equal(DayKind.Workday, shown.Sheet!.Day(Monday).Info?.Kind);
        Assert.DoesNotContain(shown.Sheet!.Day(Monday).Rows, row => row.Removed);
    }

    [Fact]
    public async Task Time_off_is_one_wait_from_the_start_until_the_period_is_read_again()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Monday));
        var requests = _h.Jira.Requests.Count;
        int? requestsWhenTheWaitStarted = null;
        shown.Waits.Changed += () => requestsWhenTheWaitStarted ??= shown.Waits.Has(JiraWait.TimeOff) ? _h.Jira.Requests.Count : null;
        _h.Jira.Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var marking = shown.MarkDayOffAsync(Monday, new TimeOffModal.TimeOff(DayOffReason.Vacation, null));

        Assert.Equal(requests, requestsWhenTheWaitStarted);
        Assert.False(shown.Waits.CanStart);
        Assert.Equal("Saving time off…", shown.Waits.Status(shown.Sheet!.Period));
        _h.Jira.Hold.SetResult();
        await marking;
        Assert.True(shown.Waits.CanStart);
        Assert.Equal(DayKind.DayOff, shown.Sheet!.Day(Monday).Info?.Kind);
    }

    [Fact]
    public async Task Quick_time_off_still_shows_its_wait_long_enough_to_be_seen()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Monday));
        var started = _h.Clock.Now;
        var ended = started;
        shown.Waits.Changed += () => { if (shown.Waits.CanStart) ended = _h.Clock.Now; };

        await shown.UnmarkDayOffAsync(Monday);

        Assert.True(ended - started >= ShownPeriod.TimeOffShownFor);
    }

    [Fact]
    public async Task Nothing_else_starts_while_time_off_is_saved()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Monday));
        _h.Jira.Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var marking = shown.MarkDayOffAsync(Monday, new TimeOffModal.TimeOff(DayOffReason.Vacation, null));
        var requests = _h.Jira.Requests.Count;

        await shown.SyncAsync();
        await shown.UnmarkDayOffAsync(Monday);
        await shown.MarkDayOffAsync(Tuesday, new TimeOffModal.TimeOff(DayOffReason.Sick, null));

        Assert.Equal(requests, _h.Jira.Requests.Count);
        Assert.False(shown.Waits.Has(JiraWait.Sync));
        _h.Jira.Hold.SetResult();
        await marking;
        Assert.Equal(DayKind.DayOff, shown.Sheet!.Day(Monday).Info?.Kind);
        Assert.Equal(DayKind.Workday, shown.Sheet!.Day(Tuesday).Info?.Kind);
    }

    [Fact]
    public async Task Time_off_waits_while_another_request_to_jira_runs()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Monday));
        _h.Jira.Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var syncing = shown.SyncAsync();

        await shown.MarkDayOffAsync(Monday, new TimeOffModal.TimeOff(DayOffReason.Vacation, null));

        _h.Jira.Hold.SetResult();
        await syncing;
        Assert.Equal(DayKind.Workday, shown.Sheet!.Day(Monday).Info?.Kind);
    }

    [Fact]
    public async Task A_period_left_before_it_was_read_is_not_shown()
    {
        var shown = NewShownPeriod();
        var november = Link(new DateOnly(2026, 11, 3));
        var early = Link(new DateOnly(2026, 10, 5));
        _h.Jira.Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var left = shown.ShowAsync(november);
        var asked = shown.ShowAsync(early);
        _h.Jira.Hold.SetResult();
        await asked;
        await left;

        Assert.Equal(early.Period, shown.Sheet!.Period);
        Assert.Equal(early.Day, shown.Day);
        Assert.True(shown.Waits.CanStart);
    }

    [Fact]
    public async Task Another_day_of_the_shown_period_keeps_the_period_and_drops_the_notice()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Monday));
        var sheet = shown.Sheet;
        shown.Notice = "About Monday";
        _h.Jira.Requests.Clear();

        await shown.ShowAsync(Link(Tuesday));

        Assert.Same(sheet, shown.Sheet);
        Assert.Equal(Tuesday, shown.Day);
        Assert.Null(shown.Notice);
        Assert.Empty(_h.Jira.Requests);
    }

    [Fact]
    public async Task A_later_period_slides_in_from_the_right_and_an_earlier_one_from_the_left()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Monday));
        Assert.Equal(0, shown.Slide);

        await shown.ShowAsync(Link(new DateOnly(2026, 11, 3)));
        Assert.Equal(1, shown.Slide);

        await shown.ShowAsync(Link(Monday));
        Assert.Equal(-1, shown.Slide);
    }

    [Fact]
    public async Task Leaving_the_period_with_own_changes_asks_first()
    {
        var shown = NewShownPeriod();
        await shown.ShowAsync(Link(Monday));
        Assert.Null(shown.LeaveWarning(null));

        Add(shown.Sheet!, Monday, "ABC-1", "2");

        Assert.Null(shown.LeaveWarning(Period.For(Tuesday))); // another day of the period
        Assert.Equal("You have unsaved changes on 1 day. They are lost if you leave now.", shown.LeaveWarning(null));
        Assert.NotNull(shown.LeaveWarning(Period.For(Monday).Next));
    }
}
