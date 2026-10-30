namespace JiraTime.Domain;

/// <summary>
/// What saving a day changes in Jira: the work removed from the day, the new and changed rows to send, and the
/// worklogs to check first. This is the one place that decides whether a day can be saved: when
/// <see cref="Errors"/> is not empty, nothing is saved. It works on copies, so the rows and saved entries it
/// was planned from never change.
/// </summary>
public sealed class DaySave
{
    /// <summary>Key in <see cref="Errors"/> for an error about the day as a whole.</summary>
    public const string DayError = "";

    /// <summary>The error on a row whose typed hours could not be read.</summary>
    public const string BadHours = "Enter the hours, e.g. 1.5 or 1:30.";

    public DateOnly Date { get; }

    /// <summary>Row id → why it is invalid; <see cref="DayError"/> for the day as a whole.</summary>
    public IReadOnlyDictionary<string, string> Errors { get; }

    /// <summary>Work that was read but is no longer among the rows: its worklogs are deleted.</summary>
    public IReadOnlyList<TimeEntry> Removed { get; }

    /// <summary>Worklogs about to be changed or deleted, as read: Jira must still have them like this.</summary>
    public IReadOnlyList<TimeEntry> ToCheck { get; }

    private readonly IReadOnlyList<(TimeEntry Row, TimeEntry? Read)> _toSend;

    private DaySave(DateOnly date, IReadOnlyDictionary<string, string> errors, IReadOnlyList<TimeEntry> removed,
        IReadOnlyList<TimeEntry> toCheck, IReadOnlyList<(TimeEntry, TimeEntry?)> toSend) =>
        (Date, Errors, Removed, ToCheck, _toSend) = (date, errors, removed, toCheck, toSend);

    /// <summary>
    /// Plans making the day match <paramref name="rows"/>, edited from <paramref name="saved"/> (the day as read).
    /// Every row is put on <paramref name="date"/> and validated first; any error leaves nothing to do.
    /// </summary>
    /// <param name="badHours">Ids of rows whose typed hours could not be read; each is an error.</param>
    public static DaySave Plan(DateOnly date, IReadOnlyList<TimeEntry> rows, IReadOnlyList<TimeEntry> saved,
        IEnumerable<string>? badHours = null)
    {
        var onDay = rows.Select(r => OnDay(r, date)).ToList();
        var errors = Validate(onDay, badHours ?? []);
        if (errors.Count > 0) return new DaySave(date, errors, [], [], []);

        var read = saved.ToDictionary(e => e.Id);
        var removed = saved.Where(e => onDay.All(r => r.Id != e.Id)).Select(e => e.Copy()).ToList();
        var toSend = onDay
            .Select(r => (Row: r, Read: read.GetValueOrDefault(r.Id)))
            .Where(p => p.Read is null || !p.Row.SameAsSynced(p.Read))
            .ToList();
        var changedAsRead = toSend.Where(p => p.Read is not null).Select(p => p.Read!);
        var toCheck = removed.Concat(changedAsRead).Where(e => e.IsSynced).ToList();
        return new DaySave(date, errors, removed, toCheck, toSend);
    }

    /// <summary>
    /// The new and changed rows, in order, as fresh copies stamped with <paramref name="nowUtc"/>. A changed row
    /// takes its Jira bookkeeping from what was read, not from the edited copy.
    /// </summary>
    public IReadOnlyList<TimeEntry> RowsToSend(DateTime nowUtc) => _toSend.Select(p => Stamped(p.Row, p.Read, nowUtc)).ToList();

    private static TimeEntry Stamped(TimeEntry row, TimeEntry? read, DateTime nowUtc)
    {
        var copy = row.Copy();
        if (read is not null)
        {
            copy.JiraWorklogId = read.JiraWorklogId;
            copy.JiraWorklogIssueKey = read.JiraWorklogIssueKey;
            copy.CreatedUtc = read.CreatedUtc;
        }
        copy.UpdatedUtc = nowUtc;
        return copy;
    }

    private static TimeEntry OnDay(TimeEntry row, DateOnly date)
    {
        var copy = row.Copy();
        copy.Date = date;
        return copy;
    }

    private static Dictionary<string, string> Validate(IReadOnlyList<TimeEntry> rows, IEnumerable<string> badHours)
    {
        var errors = new Dictionary<string, string>();
        foreach (var r in rows)
            if (TimeEntry.Validate(r) is { } error) errors[r.Id] = error;
        if (TimeEntry.Total(rows) > TimeEntry.MaxDuration)
            errors[DayError] = TimeEntry.TooManyHours;
        foreach (var id in badHours) errors[id] = BadHours;
        return errors;
    }
}
