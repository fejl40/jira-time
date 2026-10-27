namespace JiraTime.Domain;

/// <summary>Compares time entries with Jira worklogs and copies worklog values onto entries. Pure logic.</summary>
public static class WorklogMatching
{
    /// <summary>
    /// True when the entry already has the worklog's day, hours and comment (a missing comment equals an empty one).
    /// With <paramref name="includeIssue"/> the issue key must match too, ignoring case.
    /// </summary>
    public static bool Matches(this TimeEntry entry, JiraWorklog worklog, bool includeIssue = true) =>
        (!includeIssue || string.Equals(entry.IssueKey, worklog.IssueKey, StringComparison.OrdinalIgnoreCase))
        && entry.Date == worklog.Date
        && entry.Duration == worklog.Duration
        && (entry.Description ?? "") == (worklog.Comment ?? "");

    /// <summary>
    /// Copies the worklog's day, hours and comment onto the entry and marks it updated now.
    /// With <paramref name="includeIssue"/> the issue key, worklog issue key and issue summary are taken over too.
    /// </summary>
    public static void TakeFrom(this TimeEntry entry, JiraWorklog worklog, bool includeIssue = true)
    {
        if (includeIssue)
        {
            entry.IssueKey = entry.JiraWorklogIssueKey = worklog.IssueKey;
            entry.IssueSummary = worklog.IssueSummary;
        }
        entry.Date = worklog.Date;
        entry.Duration = worklog.Duration;
        entry.Description = worklog.Comment;
        entry.UpdatedUtc = DateTime.UtcNow;
    }

    /// <summary>A new synced entry for a worklog that was logged in Jira, not in this app.</summary>
    public static TimeEntry ToEntry(this JiraWorklog worklog) => new()
    {
        IssueKey = worklog.IssueKey,
        IssueSummary = worklog.IssueSummary,
        Date = worklog.Date,
        Duration = worklog.Duration,
        Description = worklog.Comment,
        JiraWorklogId = worklog.Id,
        JiraWorklogIssueKey = worklog.IssueKey,
        SyncPending = false,
        FromJira = true,
    };

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
