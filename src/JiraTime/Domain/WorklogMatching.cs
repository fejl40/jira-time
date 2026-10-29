namespace JiraTime.Domain;

/// <summary>Turns Jira worklogs into entries, lays waiting work over them and compares the two. Pure logic.</summary>
public static class WorklogMatching
{
    /// <summary>
    /// True when the entry still has the worklog's day, hours and comment (a missing comment equals an empty one).
    /// The issue is not compared: a worklog read by its issue and id is on that issue.
    /// </summary>
    public static bool Matches(this TimeEntry entry, JiraWorklog worklog) =>
        entry.Date == worklog.Date
        && entry.Duration == worklog.Duration
        && (entry.Description ?? "") == (worklog.Comment ?? "");

    /// <summary>Prefix of the ids of entries read from Jira; the rest is the worklog id.</summary>
    public const string JiraIdPrefix = "jira-";

    /// <summary>The worklog as an entry. Its id comes from the worklog's, so it stays the same on every read.</summary>
    public static TimeEntry ToEntry(this JiraWorklog worklog) => new()
    {
        Id = JiraIdPrefix + worklog.Id,
        IssueKey = worklog.IssueKey,
        IssueSummary = worklog.IssueSummary,
        Date = worklog.Date,
        Duration = worklog.Duration,
        Description = worklog.Comment,
        JiraWorklogId = worklog.Id,
        JiraWorklogIssueKey = worklog.IssueKey,
        SyncPending = false,
        CreatedUtc = worklog.Created?.UtcDateTime ?? DateTime.MinValue,
        UpdatedUtc = worklog.Created?.UtcDateTime ?? DateTime.MinValue,
    };

    /// <summary>
    /// The work as it stands: Jira's worklogs, with work Jira doesn't have yet laid over them. A waiting
    /// change or removal of a worklog replaces it; waiting new work is added. In day order, then as logged.
    /// </summary>
    public static IReadOnlyList<TimeEntry> Merge(IReadOnlyList<JiraWorklog> worklogs, IReadOnlyList<TimeEntry> waiting)
    {
        var replaced = waiting.Where(e => e.JiraWorklogId is not null).Select(e => e.JiraWorklogId!).ToHashSet();
        return worklogs.Where(w => !replaced.Contains(w.Id)).DistinctBy(w => w.Id).Select(ToEntry)
            .Concat(waiting.Where(e => !e.DeletePending))
            .OrderBy(e => e.Date).ThenBy(e => e.CreatedUtc)
            .ToList();
    }

    /// <summary>
    /// True when <paramref name="saved"/> is synced and <paramref name="edited"/> has the same issue, hours and
    /// description (compared exactly), so Jira needs nothing.
    /// </summary>
    public static bool SameAsSynced(this TimeEntry edited, TimeEntry saved) =>
        saved.IsSynced
        && saved.IssueKey == edited.IssueKey
        && saved.Duration == edited.Duration
        && saved.Description == edited.Description;
}

/// <summary>
/// What saving a day changes in Jira: the work removed from the day, the new and changed rows to send,
/// and the worklogs to check first. When <see cref="Errors"/> is not empty the day can't be saved.
/// </summary>
public sealed class DaySave
{
    /// <summary>Key in <see cref="Errors"/> for an error about the day as a whole.</summary>
    public const string DayError = "";

    /// <summary>Row id → why it is invalid; <see cref="DayError"/> for the day as a whole.</summary>
    public IReadOnlyDictionary<string, string> Errors { get; }

    /// <summary>Work that was read but is no longer among the rows: its worklogs are deleted.</summary>
    public IReadOnlyList<TimeEntry> Removed { get; }

    /// <summary>Worklogs about to be changed or deleted, as read: Jira must still have them like this.</summary>
    public IReadOnlyList<TimeEntry> ToCheck { get; }

    private readonly IReadOnlyList<(TimeEntry Row, TimeEntry? Read)> _toSend;

    private DaySave(IReadOnlyDictionary<string, string> errors, IReadOnlyList<TimeEntry> removed,
        IReadOnlyList<TimeEntry> toCheck, IReadOnlyList<(TimeEntry, TimeEntry?)> toSend) =>
        (Errors, Removed, ToCheck, _toSend) = (errors, removed, toCheck, toSend);

    /// <summary>
    /// Plans making the day match <paramref name="rows"/>, edited from <paramref name="saved"/> (the day as read).
    /// Every row is put on <paramref name="date"/> and validated first; any error leaves nothing to do.
    /// </summary>
    public static DaySave Plan(DateOnly date, IReadOnlyList<TimeEntry> rows, IReadOnlyList<TimeEntry> saved)
    {
        var errors = Validate(date, rows);
        if (errors.Count > 0) return new DaySave(errors, [], [], []);

        var read = saved.ToDictionary(e => e.Id);
        var removed = saved.Where(e => rows.All(r => r.Id != e.Id)).ToList();
        var toSend = rows
            .Select(r => (Row: r, Read: read.GetValueOrDefault(r.Id)))
            .Where(p => p.Read is null || !p.Row.SameAsSynced(p.Read))
            .ToList();
        var changedAsRead = toSend.Where(p => p.Read is not null).Select(p => p.Read!);
        var toCheck = removed.Concat(changedAsRead).Where(e => e.IsSynced).ToList();
        return new DaySave(errors, removed, toCheck, toSend);
    }

    /// <summary>
    /// The new and changed rows, in order, stamped with <paramref name="nowUtc"/>. A changed row takes its
    /// Jira bookkeeping from what was read, not from the edited copy.
    /// </summary>
    public IReadOnlyList<TimeEntry> RowsToSend(DateTime nowUtc)
    {
        foreach (var (row, read) in _toSend)
        {
            if (read is not null)
            {
                row.JiraWorklogId = read.JiraWorklogId;
                row.JiraWorklogIssueKey = read.JiraWorklogIssueKey;
                row.CreatedUtc = read.CreatedUtc;
            }
            row.UpdatedUtc = nowUtc;
        }
        return _toSend.Select(p => p.Row).ToList();
    }

    private static Dictionary<string, string> Validate(DateOnly date, IReadOnlyList<TimeEntry> rows)
    {
        var errors = new Dictionary<string, string>();
        foreach (var r in rows)
        {
            r.Date = date;
            if (TimeEntry.Validate(r) is { } error) errors[r.Id] = error;
        }
        if (TimeEntry.Total(rows) > TimeEntry.MaxDuration)
            errors[DayError] = TimeEntry.TooManyHours;
        return errors;
    }
}
