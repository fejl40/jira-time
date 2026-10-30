using System.Collections.Concurrent;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// Issues each user wrote worklogs on in the last few minutes. Jira's search finds a worklog only once
/// its index has caught up, so these issues are read directly until then (<see cref="TimeLog"/>).
/// Shared by every tab, so a save in one tab shows in the others too.
/// </summary>
public sealed class RecentWorklogIssues(TimeProvider clock)
{
    /// <summary>How long an issue is read directly after a worklog was written on it.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, ExpiringCache<JiraIssue>> _byOwner = new();

    public void Add(string owner, JiraIssue issue)
    {
        var issues = _byOwner.GetOrAdd(owner, _ => new(clock, Window, StringComparer.OrdinalIgnoreCase));
        issues.Set(issue.Key, new JiraIssue(issue.Key, issue.Summary));
    }

    /// <summary>The owner's issues written on within <see cref="Window"/>.</summary>
    public IReadOnlyList<JiraIssue> For(string owner) =>
        _byOwner.TryGetValue(owner, out var issues) ? issues.Values : [];
}
