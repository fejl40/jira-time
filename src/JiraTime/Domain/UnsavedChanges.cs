namespace JiraTime.Domain;

/// <summary>What Save all does to a row: adds it, changes it, or removes it.</summary>
public enum RowChangeKind { Added, Changed, Removed }

/// <summary>
/// One row Save all changes in Jira: the issue and hours it will have (a removed row: as it was saved).
/// A changed row also says what it was before, where that differs.
/// </summary>
public sealed record RowChange(RowChangeKind Kind, string IssueKey, string IssueSummary, TimeSpan Hours)
{
    public string? Comment { get; init; }
    /// <summary>A changed row moved to another issue: the issue it was on.</summary>
    public string? IssueBefore { get; init; }
    /// <summary>A changed row got other hours: the hours it had.</summary>
    public TimeSpan? HoursBefore { get; init; }
    public bool CommentChanged { get; init; }
    /// <summary>A removed row Jira has: saving deletes its worklog.</summary>
    public bool DeletesWorklog { get; init; }
}

/// <summary>One unsaved day as Save all will save it: its row changes and hours before and after.</summary>
public sealed record DayChanges(DateOnly Day, IReadOnlyList<RowChange> Rows, TimeSpan Before, TimeSpan After)
{
    /// <summary>Why Save all will leave the day unsaved (a row needs an issue or hours); null when it saves.</summary>
    public string? Problem { get; init; }

    public const string BadHours = "Enter the hours, e.g. 1.5 or 1:30.";

    /// <summary>The day's changes, made from its unsaved work and its saved rows.</summary>
    public static DayChanges Of(DayWork work, IReadOnlyList<TimeEntry> saved)
    {
        var rows = new List<RowChange>();
        foreach (var entry in saved)
        {
            if (work.IsRemoved(entry)) rows.Add(Removed(entry));
            else if (work.IsChanged(entry)) rows.Add(Changed(entry, work.View(entry)));
        }
        foreach (var draft in work.Drafts.Where(d => !work.IsBlank(d)))
            rows.Add(new RowChange(RowChangeKind.Added, draft.IssueKey, draft.IssueSummary, draft.Duration) { Comment = draft.Description });

        var problem = Errors(work, saved).Values.FirstOrDefault();
        return new DayChanges(work.Date, rows, TimeEntry.Total(saved), work.Total(saved)) { Problem = problem };
    }

    /// <summary>
    /// Why the day's rows can't be saved: row id → what to fix, and <see cref="DaySave.DayError"/> for the
    /// day as a whole. Empty when the day can be saved.
    /// </summary>
    public static Dictionary<string, string> Errors(DayWork work, IReadOnlyList<TimeEntry> saved)
    {
        var errors = DaySave.Plan(work.Date, work.RowsToSave(saved), saved).Errors.ToDictionary();
        foreach (var id in work.BadHours.Keys) errors[id] = BadHours;
        return errors;
    }

    private static RowChange Removed(TimeEntry entry) =>
        new(RowChangeKind.Removed, entry.IssueKey, entry.IssueSummary, entry.Duration)
        {
            Comment = entry.Description,
            DeletesWorklog = entry.JiraWorklogId is not null,
        };

    private static RowChange Changed(TimeEntry before, TimeEntry now) =>
        new(RowChangeKind.Changed, now.IssueKey, now.IssueSummary, now.Duration)
        {
            Comment = now.Description,
            IssueBefore = now.IssueKey != before.IssueKey ? before.IssueKey : null,
            HoursBefore = now.Duration != before.Duration ? before.Duration : null,
            CommentChanged = (now.Description ?? "") != (before.Description ?? ""),
        };
}

/// <summary>Everything Save all will save: each unsaved day of a period with its changes, and the totals.</summary>
public sealed record UnsavedChanges(IReadOnlyList<DayChanges> Days)
{
    /// <summary>The unsaved days of <paramref name="sheet"/>, in order.</summary>
    public static UnsavedChanges Of(PeriodSheet sheet) =>
        new(sheet.UnsavedDays.Select(day => DayChanges.Of(sheet.WorkFor(day), sheet.SavedOn(day))).ToList());

    /// <summary>Days Save all will save; the others have a <see cref="DayChanges.Problem"/>.</summary>
    public IReadOnlyList<DayChanges> ToSave => Days.Where(d => d.Problem is null).ToList();

    public int Count(RowChangeKind kind) => ToSave.Sum(d => d.Rows.Count(r => r.Kind == kind));

    /// <summary>Jira worklogs that saving deletes.</summary>
    public int DeletedWorklogs => ToSave.Sum(d => d.Rows.Count(r => r.DeletesWorklog));

    /// <summary>Hours the days to save have now, and will have once saved.</summary>
    public TimeSpan Before => ToSave.Select(d => d.Before).Sum();
    public TimeSpan After => ToSave.Select(d => d.After).Sum();
}
