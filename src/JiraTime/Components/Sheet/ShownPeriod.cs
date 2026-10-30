using JiraTime.Components.Sheet;
using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Components;

/// <summary>
/// The period the timesheet shows and its work with Jira: opening it, reading it again, Try again, Sync now,
/// Save all and time off. Timesheet.razor keeps the URL (selecting a day), modals, focus and animations.
/// One per page (registered transient), so a page opened again starts fresh.
/// </summary>
public sealed class ShownPeriod(
    PeriodLoader loader, IssueStatuses statuses, WorklogSync sync, TimeLog log,
    WorkCalendarService calendar, CurrentUser user, TimeProvider clock)
{
    /// <summary>Read on each use, so a tab left open overnight moves on to the new day.</summary>
    public DateOnly Today => clock.Today();

    /// <summary>The period shown, with its unsaved changes; null until the first period has loaded.</summary>
    public PeriodSheet? Sheet { get; private set; }

    /// <summary>
    /// What goes around the period: why Jira couldn't be read (the period then shows only work waiting for
    /// Jira and can't be edited), the previous period's state, and what the issue picker offers.
    /// </summary>
    public PeriodExtras Extras { get; private set; } = new(null, null, [], new Dictionary<string, IReadOnlyList<string>>(), []);

    /// <summary>Requests to Jira that are running; parts of the page show them as pending.</summary>
    public JiraWaits Waits { get; } = new();

    /// <summary>News about the selected day, such as days Save all couldn't save.</summary>
    public string? Notice { get; set; }

    /// <summary>The last save, for its animation.</summary>
    public SaveMotion SaveMotion { get; } = new();

    /// <summary>The issue statuses of the period (rows on Done issues get a note) and the issue picker's search.</summary>
    public IssueStatuses Statuses => statuses;

    /// <summary>Which way the last period change went: 1 to a later period, -1 to an earlier one, 0 on first show.</summary>
    public int Slide { get; private set; }

    public bool HasUnsaved => Sheet?.OwnChangeDays.Count > 0;

    private PeriodSheet Shown => Sheet ?? throw new InvalidOperationException("No period is shown yet.");

    /// <summary>The day shown in the day panel, as the URL says; <see cref="ShowAsync"/> moves it.</summary>
    public DateOnly Day { get; private set; }

    /// <summary>
    /// Shows the link's period and day. Another day of the shown period keeps everything; another period is read
    /// while the old one stays on screen, and isn't shown when another one was asked for meanwhile.
    /// </summary>
    public async Task ShowAsync(TimesheetLink link)
    {
        var day = link.Selected(Today);
        if (Sheet?.Period == link.Period)
        {
            ShowDay(day);
            return;
        }
        var sheet = new PeriodSheet(link.Period, () => Today);
        if (await Waits.OpenAsync(link.Period, () => loader.LoadAsync(sheet, statuses)) is not { } loaded) return;
        Slide = Sheet is null ? 0 : link.Period.Start.CompareTo(Sheet.Period.Start);
        (Sheet, Extras, Day, Notice) = (sheet, loaded, day, null);
        sheet.Suggest(day);
    }

    private void ShowDay(DateOnly day)
    {
        if (day == Day) return;
        (Day, Notice) = (day, null);
        Shown.Suggest(day);
    }

    /// <summary>Reads the shown period again, keeping its unsaved changes.</summary>
    public Task ReloadAsync() =>
        Waits.RunAsync(JiraWait.Period, async () => Extras = await loader.LoadAsync(Shown, statuses));

    /// <summary>Try again after Jira couldn't be read: reads Jira afresh and suggests work for the shown day.</summary>
    public async Task RetryAsync()
    {
        log.Refresh();
        await ReloadAsync();
        Shown.Suggest(Day);
    }

    /// <summary>Discard all: drops every day's unsaved changes, and the news about them.</summary>
    public void DiscardAll()
    {
        Shown.DiscardAll();
        Notice = null;
    }

    /// <summary>
    /// What to ask before leaving for <paramref name="target"/> (null when it isn't a timesheet link):
    /// null when nothing unsaved would be lost, as for another day of the shown period.
    /// </summary>
    public string? LeaveWarning(Period? target)
    {
        if (!HasUnsaved || target == Shown.Period) return null;
        return $"You have unsaved changes on {Format.Days(Shown.OwnChangeDays.Count)}. They are lost if you leave now.";
    }

    /// <summary>Sync now: sends the retry queue and reads what was logged in Jira meanwhile.</summary>
    public async Task SyncAsync()
    {
        if (user.Jira is null || !Waits.CanStart) return;
        using var action = JiraAction.Start("Sync now");
        await Waits.RunAsync(JiraWait.Sync, async () =>
        {
            await sync.SyncRangeAsync(Shown.Period.Start, Shown.Period.End);
            log.Refresh();
            await ReloadAsync();
        });
    }

    /// <summary>
    /// Saves every day with unsaved changes; <paramref name="beforeReload"/> runs once the saved days are finished,
    /// before the period is read again. Returns the first day that couldn't be saved, for the page to select, with
    /// <see cref="Notice"/> saying why; null when every day was saved.
    /// </summary>
    public async Task<DateOnly?> SaveDaysAsync(Func<Task> beforeReload)
    {
        if (user.Jira is null) return null;
        using var action = JiraAction.Start("Save all");
        Notice = null;
        SaveMotion.Start();
        WorklogSync.SaveAllResult? result = null;
        await Waits.RunAsync(JiraWait.SaveAll, async () =>
        {
            result = await sync.SaveDaysAsync(Shown.PlanSaveAll());
            foreach (var (day, dayResult) in result.Saved)
            {
                Shown.Finish(day);
                SaveMotion.Saved(day, sentToJira: dayResult.SyncFailures == 0);
            }
            await beforeReload();
            await ReloadAsync();
        });
        SaveMotion.Finish();
        if (result!.FirstToFix is not { } toFix) return null;
        Notice = result.Notice;
        return toFix;
    }

    /// <summary>Marks the day, or part of it, off; PeriodSheet decides what happens to the day's work.</summary>
    public async Task MarkDayOffAsync(DateOnly day, TimeOffModal.TimeOff off)
    {
        using var action = JiraAction.Start($"Time off {Format.ShortDay(day)}");
        await Shown.MarkDayOffAsync(day, wholeDay: off.Hours is null, async () =>
        {
            await calendar.MarkDayOffAsync(day, off.Reason, off.Hours);
            await ReloadAsync();
        });
    }

    public async Task UnmarkDayOffAsync(DateOnly day)
    {
        using var action = JiraAction.Start($"Remove time off {Format.ShortDay(day)}");
        await Shown.UnmarkDayOffAsync(day, async () =>
        {
            await calendar.UnmarkDayOffAsync(day);
            await ReloadAsync();
        });
    }
}

/// <summary>The last save, for its animation: days saved, and those Jira got completely.</summary>
public sealed class SaveMotion
{
    private bool _flyTicks;

    public HashSet<DateOnly> JustSaved { get; } = [];
    public HashSet<DateOnly> SentToJira { get; } = [];

    /// <summary>A save begins: the last one's days no longer animate.</summary>
    public void Start()
    {
        JustSaved.Clear();
        SentToJira.Clear();
    }

    public void Saved(DateOnly day, bool sentToJira)
    {
        JustSaved.Add(day);
        if (sentToJira) SentToJira.Add(day);
    }

    /// <summary>Called after the saved work has reloaded, so the ticks start from the saved cells.</summary>
    public void Finish() => _flyTicks = SentToJira.Count > 0;

    /// <summary>True once after a save that sent days to Jira: time to fly the ticks.</summary>
    public bool TakeFlyTicks()
    {
        var fly = _flyTicks;
        _flyTicks = false;
        return fly;
    }
}
