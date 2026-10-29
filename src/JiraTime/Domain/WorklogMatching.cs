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
