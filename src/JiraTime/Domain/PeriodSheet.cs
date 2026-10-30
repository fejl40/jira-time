namespace JiraTime.Domain;

/// <summary>One issue's row in the period grid: its saved entries in the period.</summary>
public sealed record PeriodRow(string Key, string Summary, IReadOnlyList<TimeEntry> Entries)
{
    /// <summary>The row's saved entries on one day.</summary>
    public IReadOnlyList<TimeEntry> On(DateOnly day) => Entries.Where(e => e.Date == day).ToList();
}

/// <summary>
/// One timesheet period as the user works on it: saved entries and days, plus each day's unsaved changes (read and
/// edited through <see cref="Day"/>) until they are saved or discarded. Pure logic: the page loads and saves.
/// </summary>
public sealed class PeriodSheet(Period period, Func<DateOnly> today)
{
    private readonly Dictionary<DateOnly, DayWork> _work = [];
    /// <summary>Days the user saved or discarded; they are not suggested again until the page reloads.</summary>
    private readonly HashSet<DateOnly> _finished = [];

    public Period Period { get; } = period;
    public DateOnly Today => today();

    /// <summary>Saved entries in the period.</summary>
    public IReadOnlyList<TimeEntry> Entries { get; private set; } = [];
    /// <summary>Saved entries in the weeks before the period, to copy from.</summary>
    public IReadOnlyList<TimeEntry> Before { get; private set; } = [];
    public IReadOnlyDictionary<DateOnly, DayInfo> Days { get; private set; } = new Dictionary<DateOnly, DayInfo>();
    /// <summary>The user's sprint issues, used to guess the issue of a suggestion.</summary>
    public IReadOnlyList<IssueWithStatus> Sprint { get; set; } = [];

    /// <summary>False when Jira couldn't be read: <see cref="Entries"/> is then only the work waiting for Jira, so nothing counts as missing or is suggested.</summary>
    public bool HoursKnown { get; private set; } = true;

    /// <summary>Takes freshly loaded saved data. Unsaved changes are kept.</summary>
    public void Load(IReadOnlyList<TimeEntry> entries, IReadOnlyList<TimeEntry> before, IReadOnlyDictionary<DateOnly, DayInfo> days, bool hoursKnown = true)
    {
        (Entries, Before, Days, HoursKnown) = (entries, before, days, hoursKnown);
        _summary = null;
    }

    /// <summary>The saved data's summary, made again after a load or when the day changes.</summary>
    private (DateOnly Today, PeriodSummary Summary)? _summary;

    /// <summary>Where the saved data stands: logged, missing (per day too), not in Jira.</summary>
    public PeriodSummary Summary
    {
        get
        {
            var today = Today;
            if (_summary is not { } cached || cached.Today != today)
                _summary = cached = (today, PeriodSummary.For(Period, Days, Entries, today, HoursKnown));
            return cached.Summary;
        }
    }
    /// <summary>The day's kind and hours; null for a day that wasn't loaded.</summary>
    public DayInfo? Info(DateOnly day) => Days.GetValueOrDefault(day);
    public IReadOnlyList<TimeEntry> SavedOn(DateOnly day) => Entries.Where(e => e.Date == day).ToList();
    /// <summary>Only workdays up to today with hours missing can be filled.</summary>
    public bool CanFill(DateOnly day) => Day(day).Missing > TimeSpan.Zero;

    /// <summary>The day with its saved rows and unsaved changes, to read and edit.</summary>
    public SheetDay Day(DateOnly day) => new(this, day);
    internal DayWork? WorkOn(DateOnly day) => _work.GetValueOrDefault(day);
    internal DayWork WorkToChange(DateOnly day) => _work.TryGetValue(day, out var w) ? w : _work[day] = new DayWork(day);

    /// <summary>Days with unsaved work of any kind.</summary>
    private IEnumerable<SheetDay> Edited => _work.Keys.Select(Day);

    /// <summary>Days with changes that are not saved, in order.</summary>
    public IReadOnlyList<DateOnly> UnsavedDays => Edited.Where(d => d.HasChanges).Select(d => d.Date).Order().ToList();
    public bool IsUnsaved(DateOnly day) => Day(day).HasChanges;
    /// <summary>Days with changes the user made (untouched suggestions don't count).</summary>
    public IReadOnlyList<DateOnly> OwnChangeDays => Edited.Where(d => d.HasOwnChanges).Select(d => d.Date).ToList();
    /// <summary>Hours the unsaved days will have once saved.</summary>
    public TimeSpan UnsavedTotal => UnsavedDays.Select(day => Day(day).Total).Sum();
    /// <summary>Hours an issue will have on a day once saved, when the day has unsaved changes for it; otherwise null.</summary>
    public TimeSpan? UnsavedFor(DateOnly day, string issueKey) => Day(day).UnsavedFor(issueKey);

    /// <summary>What Save all sends: a save plan for every unsaved day, in order. A day that can't be saved has errors.</summary>
    public IReadOnlyList<DaySave> PlanSaveAll() => UnsavedDays.Select(day => Day(day).Plan()).ToList();

    /// <summary>One row per issue with saved or unsaved time in the period; issues by key, a row without an issue last.</summary>
    public IReadOnlyList<PeriodRow> Rows
    {
        get
        {
            var unsaved = Edited.SelectMany(d => d.RowsToSave).ToList();
            var keys = Entries.Concat(unsaved).Select(e => e.IssueKey).Distinct().OrderBy(k => k == "").ThenBy(k => k, StringComparer.OrdinalIgnoreCase);
            return keys.Select(key => RowOf(key, unsaved)).ToList();
        }
    }

    /// <summary>An issue's grid row: its saved entries, named after the latest saved entry (or the latest unsaved one).</summary>
    private PeriodRow RowOf(string key, IReadOnlyList<TimeEntry> unsaved)
    {
        var saved = Entries.Where(e => e.IssueKey == key).ToList();
        var named = saved.MaxBy(e => e.UpdatedUtc) ?? unsaved.Where(e => e.IssueKey == key).MaxBy(e => e.UpdatedUtc)!;
        return new PeriodRow(key, named.IssueSummary, saved);
    }

    /// <summary>The latest day before this one with work, to copy: its rows as they will be once saved (suggestions too).</summary>
    public (DateOnly Date, IReadOnlyList<TimeEntry> Rows)? PreviousWorkedDay(DateOnly day) =>
        DayFiller.PreviousWorkedDay(day, d => Period.Contains(d) ? Day(d).RowsToSave : Before.Where(e => e.Date == d).ToList());

    /// <summary>Suggests the missing hours of a short workday, unless the day has unsaved changes or the user saved or discarded it.</summary>
    public void Suggest(DateOnly day)
    {
        if (CanFill(day) && !_finished.Contains(day)) Day(day).Suggest();
    }

    /// <summary>Suggests work for every short workday so far, including days saved or discarded before.</summary>
    public void FillPeriod()
    {
        _finished.ExceptWith(Period.Days);
        foreach (var day in Period.Days) Suggest(day);
    }

    /// <summary>The day was saved or its changes discarded: drop them and don't suggest the day again.</summary>
    public void Finish(DateOnly day)
    {
        _work.Remove(day);
        _finished.Add(day);
    }

    public void DiscardAll()
    {
        foreach (var day in UnsavedDays) Finish(day);
    }

    /// <summary>Starts the day over: unsaved changes are dropped and a fresh suggestion is made.</summary>
    public void Restart(DateOnly day)
    {
        _work.Remove(day);
        _finished.Remove(day);
        Suggest(day);
    }

    /// <summary>The day's row for an issue, for a grid cell: the row it has (suggesting work first, as opening the day would), or a new empty row.</summary>
    public TimeEntry RowFor(DateOnly day, string issueKey, string issueSummary)
    {
        Suggest(day);
        return Day(day).RowOn(issueKey) ?? Day(day).AddRow(new JiraIssue(issueKey, issueSummary));
    }

    /// <summary>
    /// Marks the day off around <paramref name="saveAndReload"/> (stores it, reads the period again). A whole day off marks
    /// the saved work removed, so Save all deletes it from Jira. Part of a day off remakes an untouched suggestion for the
    /// new hours (dropping it first, so it doesn't show meanwhile); changes the user made are kept.
    /// </summary>
    public async Task MarkDayOffAsync(DateOnly day, bool wholeDay, Func<Task> saveAndReload)
    {
        var onlySuggestions = Day(day).HasOnlySuggestions;
        if (!wholeDay && onlySuggestions) Finish(day);
        await saveAndReload();
        if (wholeDay) Day(day).TakeOff();
        else if (onlySuggestions) Restart(day);
    }

    /// <summary>Removes the day off around <paramref name="saveAndReload"/>: saved work marked removed comes back, and an untouched day is suggested again.</summary>
    public async Task UnmarkDayOffAsync(DateOnly day, Func<Task> saveAndReload)
    {
        await saveAndReload();
        Day(day).UndoTakeOff();
        if (Day(day).HasOnlySuggestions) Restart(day);
    }
}
