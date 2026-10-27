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

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, (string Summary, DateTimeOffset At)>> _byOwner = new();

    public void Add(string owner, JiraIssue issue) =>
        _byOwner.GetOrAdd(owner, _ => new(StringComparer.OrdinalIgnoreCase))[issue.Key] = (issue.Summary, clock.GetUtcNow());

    /// <summary>The owner's issues written on within <see cref="Window"/>.</summary>
    public IReadOnlyList<JiraIssue> For(string owner)
    {
        if (!_byOwner.TryGetValue(owner, out var issues)) return [];
        var since = clock.GetUtcNow() - Window;
        foreach (var old in issues.Where(i => i.Value.At < since)) issues.TryRemove(old);
        return issues.Select(i => new JiraIssue(i.Key, i.Value.Summary)).ToList();
    }
}
