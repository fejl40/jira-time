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

    /// <summary>The day's changes, made from its saved rows and unsaved work.</summary>
    public static DayChanges Of(SheetDay day)
    {
        var rows = new List<RowChange>();
        foreach (var row in day.Rows)
        {
            if (row.State == RowState.Removed) rows.Add(Removed(row.Saved!));
            else if (row.State == RowState.Changed) rows.Add(Changed(row.Saved!, row.Entry));
        }
        var savedIds = day.Saved.Select(e => e.Id).ToHashSet();
        foreach (var added in day.RowsToSave.Where(r => !savedIds.Contains(r.Id)))
            rows.Add(new RowChange(RowChangeKind.Added, added.IssueKey, added.IssueSummary, added.Duration) { Comment = added.Comment });

        var problem = day.Errors.Values.FirstOrDefault();
        return new DayChanges(day.Date, rows, day.Logged, day.Total) { Problem = problem };
    }

    private static RowChange Removed(TimeEntry entry) =>
        new(RowChangeKind.Removed, entry.IssueKey, entry.IssueSummary, entry.Duration)
        {
            Comment = entry.Comment,
            DeletesWorklog = entry.JiraWorklogId is not null,
        };

    private static RowChange Changed(TimeEntry before, TimeEntry now) =>
        new(RowChangeKind.Changed, now.IssueKey, now.IssueSummary, now.Duration)
        {
            Comment = now.Comment,
            IssueBefore = now.IssueKey != before.IssueKey ? before.IssueKey : null,
            HoursBefore = now.Duration != before.Duration ? before.Duration : null,
            CommentChanged = !TimeEntry.SameComment(now.Comment, before.Comment),
        };
}

/// <summary>Everything Save all will save: each unsaved day of a period with its changes, and the totals.</summary>
public sealed record UnsavedChanges(IReadOnlyList<DayChanges> Days)
{
    /// <summary>The unsaved days of <paramref name="sheet"/>, in order.</summary>
    public static UnsavedChanges Of(PeriodSheet sheet) =>
        new(sheet.UnsavedDays.Select(day => DayChanges.Of(sheet.Day(day))).ToList());

    /// <summary>Days Save all will save; the others have a <see cref="DayChanges.Problem"/>.</summary>
    public IReadOnlyList<DayChanges> ToSave => Days.Where(d => d.Problem is null).ToList();

    public int Count(RowChangeKind kind) => ToSave.Sum(d => d.Rows.Count(r => r.Kind == kind));

    /// <summary>Jira worklogs that saving deletes.</summary>
    public int DeletedWorklogs => ToSave.Sum(d => d.Rows.Count(r => r.DeletesWorklog));

    /// <summary>Hours the days to save have now, and will have once saved.</summary>
    public TimeSpan Before => ToSave.Select(d => d.Before).Sum();
    public TimeSpan After => ToSave.Select(d => d.After).Sum();
}
