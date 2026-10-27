using System.Collections.Concurrent;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// The user's issues from Jira (active sprint, earlier in the period, statuses of given keys),
/// cached briefly so filling a whole period asks Jira once.
/// </summary>
public class SprintIssues(JiraClient jira, TimeProvider clock)
{
    public static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, (DateTimeOffset At, IReadOnlyList<SprintIssue> Issues)> _cache = new();
    private readonly ConcurrentDictionary<string, (DateTimeOffset At, SprintIssue? Issue)> _statuses = new();

    /// <summary>Issues assigned to the user that were To Do or In Progress during <paramref name="period"/>.</summary>
    public async Task<IReadOnlyList<SprintIssue>> GetPeriodAsync(JiraConnection c, JiraProfile profile, Period period, bool refresh = false, CancellationToken ct = default)
    {
        var key = $"{c.BaseUrl}|{profile.Id}|{period.Start:yyyy-MM-dd}";
        if (!refresh && _cache.TryGetValue(key, out var hit) && clock.GetUtcNow() - hit.At < CacheFor)
            return hit.Issues;

        var issues = await jira.GetPeriodIssuesAsync(c, profile, period.Start, period.End, ct);
        _cache[key] = (clock.GetUtcNow(), issues);
        return issues;
    }

    /// <summary>Current status of each key Jira knows; looked up once per key while cached.</summary>
    public async Task<IReadOnlyDictionary<string, SprintIssue>> GetStatusesAsync(JiraConnection c, IEnumerable<string> keys, CancellationToken ct = default)
    {
        string Key(string issue) => $"{c.BaseUrl}|{issue.ToUpperInvariant()}";
        var wanted = keys.Where(k => k != "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var now = clock.GetUtcNow();
        var missing = wanted.Where(k => !_statuses.TryGetValue(Key(k), out var hit) || now - hit.At >= CacheFor).ToList();
        if (missing.Count > 0)
        {
            var found = (await jira.GetIssuesAsync(c, missing, ct)).ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var k in missing) _statuses[Key(k)] = (now, found.GetValueOrDefault(k));
        }
        return wanted
            .Select(k => _statuses.TryGetValue(Key(k), out var hit) ? hit.Issue : null)
            .OfType<SprintIssue>()
            .ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
    }

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
