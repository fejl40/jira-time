using System.Collections.Concurrent;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>The user's active-sprint issues, cached briefly so filling a whole period asks Jira once.</summary>
public class SprintIssues(JiraClient jira, TimeProvider clock)
{
    public static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, (DateTimeOffset At, IReadOnlyList<SprintIssue> Issues)> _cache = new();

    public async Task<IReadOnlyList<SprintIssue>> GetAsync(JiraConnection c, JiraProfile profile, bool refresh = false, CancellationToken ct = default)
    {
        var key = $"{c.BaseUrl}|{profile.Id}";
        if (!refresh && _cache.TryGetValue(key, out var hit) && clock.GetUtcNow() - hit.At < CacheFor)
            return hit.Issues;

        var issues = await jira.GetSprintIssuesAsync(c, profile, ct);
        _cache[key] = (clock.GetUtcNow(), issues);
        return issues;
    }
}
