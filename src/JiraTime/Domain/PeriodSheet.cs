namespace JiraTime.Domain;

/// <summary>One issue's row in the period grid: its saved entries in the period.</summary>
public sealed record PeriodRow(string Key, string Summary, IReadOnlyList<TimeEntry> Entries);

/// <summary>
/// One timesheet period as the user works on it: the saved entries and days, plus unsaved changes per
/// day (edits, removals, suggestions and added issues) until they are saved or discarded. Pure logic:
/// the page loads the data and saves the days.
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

    /// <summary>Takes freshly loaded saved data. Unsaved changes are kept.</summary>
    public void Load(IReadOnlyList<TimeEntry> entries, IReadOnlyList<TimeEntry> before, IReadOnlyDictionary<DateOnly, DayInfo> days)
    {
        Entries = entries;
        Before = before;
        Days = days;
    }

    public PeriodSummary Summary => PeriodSummary.For(Period, Days, Entries, Today);

    public DayInfo? Day(DateOnly day) => Days.GetValueOrDefault(day);

    public IReadOnlyList<TimeEntry> SavedOn(DateOnly day) => Entries.Where(e => e.Date == day).ToList();

    public static TimeSpan Total(IEnumerable<TimeEntry> entries) => entries.Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration);

    public static bool HasSyncProblem(IEnumerable<TimeEntry> entries) => entries.Any(e => !e.IsSynced);

    /// <summary>Hours missing on a day; only counted up to today.</summary>
    public TimeSpan Missing(DateOnly day) =>
        day > Today || !Days.TryGetValue(day, out var info) ? TimeSpan.Zero : info.MissingWith(Total(SavedOn(day)));

    /// <summary>Only workdays up to today with hours missing can be filled.</summary>
    public bool CanFill(DateOnly day) => day <= Today && Missing(day) > TimeSpan.Zero;

    // ---- Unsaved work ----

    /// <summary>The day's unsaved changes, made on first use.</summary>
    public DayWork WorkFor(DateOnly day) => _work.TryGetValue(day, out var w) ? w : _work[day] = new DayWork(day);

    /// <summary>Days with changes that are not saved, in order.</summary>
    public IReadOnlyList<DateOnly> UnsavedDays =>
        _work.Where(w => w.Value.HasChanges(SavedOn(w.Key))).Select(w => w.Key).Order().ToList();

    public bool IsUnsaved(DateOnly day) => _work.TryGetValue(day, out var w) && w.HasChanges(SavedOn(day));

    /// <summary>Days with changes the user made (untouched suggestions don't count).</summary>
    public IReadOnlyList<DateOnly> OwnChangeDays =>
        _work.Where(w => w.Value.HasOwnChanges(SavedOn(w.Key))).Select(w => w.Key).ToList();

    /// <summary>Hours the unsaved days will have once saved.</summary>
    public TimeSpan UnsavedTotal => UnsavedDays.Aggregate(TimeSpan.Zero, (t, d) => t + UnsavedDayTotal(d));

    /// <summary>Hours a day will have once its changes are saved.</summary>
    public TimeSpan UnsavedDayTotal(DateOnly day) => WorkFor(day).Total(SavedOn(day));

    /// <summary>Jira worklogs that saving the unsaved days deletes.</summary>
    public int UnsavedRemovals =>
        UnsavedDays.Sum(d => SavedOn(d).Count(e => _work[d].IsRemoved(e) && e.JiraWorklogId is not null));

    /// <summary>Hours an issue will have on a day once saved, when the day has unsaved changes for it; otherwise null.</summary>
    public TimeSpan? UnsavedFor(DateOnly day, string issueKey)
    {
        if (!_work.TryGetValue(day, out var work)) return null;
        var saved = SavedOn(day);
        if (!work.HasChanges(saved)) return null;
        var after = work.TotalFor(saved, issueKey);
        return after == Total(saved.Where(e => e.IssueKey == issueKey)) ? null : after;
    }

    /// <summary>One row per issue with saved or unsaved time in the period; issues by key, a row without an issue last.</summary>
    public IReadOnlyList<PeriodRow> Rows
    {
        get
        {
            var unsaved = _work.Values.SelectMany(w => w.RowsToSave(SavedOn(w.Date)));
            return Entries.Select(e => (Entry: e, Saved: true))
                .Concat(unsaved.Select(e => (Entry: e, Saved: false)))
                .GroupBy(x => x.Entry.IssueKey)
                .Select(g => new PeriodRow(g.Key,
                    g.OrderByDescending(x => x.Saved).ThenByDescending(x => x.Entry.UpdatedUtc).First().Entry.IssueSummary,
                    g.Where(x => x.Saved).Select(x => x.Entry).ToList()))
                .OrderBy(r => r.Key == "").ThenBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    /// <summary>
    /// Puts a suggestion for the missing hours on a short workday, unless the day has unsaved changes or
    /// the user saved or discarded it.
    /// </summary>
    public void Suggest(DateOnly day)
    {
        if (!CanFill(day) || _finished.Contains(day) || !Days.TryGetValue(day, out var info)) return;
        var work = WorkFor(day);
        if (work.HasChanges(SavedOn(day)) || work.Drafts.Count > 0) return;
        var drafts = DayFiller.Propose(info, SavedOn(day), Sprint, PreviousWorkedDay(day)?.Rows);
        foreach (var d in drafts) work.Suggested.Add(d.Id);
        work.Drafts.AddRange(drafts);
    }

    /// <summary>Suggests work for every short workday so far, including days saved or discarded before.</summary>
    public void FillPeriod()
    {
        foreach (var day in Period.Days)
        {
            _finished.Remove(day);
            Suggest(day);
        }
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

    /// <summary>True when the day's only unsaved changes are untouched suggestions (or there are none).</summary>
    public bool HasOnlySuggestions(DateOnly day)
    {
        if (!_work.TryGetValue(day, out var work)) return true;
        return work.Drafts.All(d => work.Suggested.Contains(d.Id)) && !SavedOn(day).Any(work.IsChanged) && work.Removed.Count == 0;
    }

    /// <summary>A day's rows as they will be once saved: saved work plus unsaved changes (suggestions too).</summary>
    public IReadOnlyList<TimeEntry> RowsOn(DateOnly day) =>
        !Period.Contains(day) ? Before.Where(e => e.Date == day).ToList()
        : _work.TryGetValue(day, out var w) ? w.RowsToSave(SavedOn(day))
        : SavedOn(day);

    /// <summary>The latest day before this one with work, to copy.</summary>
    public (DateOnly Date, IReadOnlyList<TimeEntry> Rows)? PreviousWorkedDay(DateOnly day) => DayFiller.PreviousWorkedDay(day, RowsOn);

    /// <summary>
    /// The day's row for an issue, for a click on a grid cell: the row it has (suggesting work first, as
    /// opening the day would), or a new empty row on the issue.
    /// </summary>
    public TimeEntry RowFor(DateOnly day, string issueKey, string issueSummary)
    {
        Suggest(day);
        var work = WorkFor(day);
        var existing = SavedOn(day).Where(e => !work.IsRemoved(e)).Select(work.View)
            .Concat(work.Drafts)
            .FirstOrDefault(e => string.Equals(e.IssueKey, issueKey, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;
        var row = new TimeEntry { IssueKey = issueKey, IssueSummary = issueSummary, Date = day };
        work.Drafts.Add(row);
        return row;
    }
}
