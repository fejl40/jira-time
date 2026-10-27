using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// The user's issues from Jira (active sprint, earlier in a period) and the current status of given
/// issues, cached briefly so filling a whole period asks Jira once.
/// </summary>
public class JiraIssueLookup(JiraClient jira, TimeProvider clock)
{
    public static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(5);

    private readonly ExpiringCache<IReadOnlyList<IssueWithStatus>> _lists = new(clock, CacheFor);
    private readonly ExpiringCache<IssueWithStatus?> _statuses = new(clock, CacheFor);

    /// <summary>Issues assigned to the user in an open sprint that are not Done.</summary>
    public Task<IReadOnlyList<IssueWithStatus>> GetSprintAsync(JiraConnection c, JiraProfile profile, bool refresh = false, CancellationToken ct = default) =>
        _lists.GetOrLoadAsync($"{c.BaseUrl}|{profile.Id}",
            () => jira.GetSprintIssuesAsync(c, profile, ct), refresh);

    /// <summary>Issues assigned to the user that were To Do or In Progress during <paramref name="period"/>.</summary>
    public Task<IReadOnlyList<IssueWithStatus>> GetPeriodAsync(JiraConnection c, JiraProfile profile, Period period, bool refresh = false, CancellationToken ct = default) =>
        _lists.GetOrLoadAsync($"{c.BaseUrl}|{profile.Id}|{period.Start:yyyy-MM-dd}",
            () => jira.GetPeriodIssuesAsync(c, profile, period.Start, period.End, ct), refresh);

    /// <summary>Current status of each key Jira knows; looked up once per key while cached, unknown keys included.</summary>
    public async Task<IReadOnlyDictionary<string, IssueWithStatus>> GetStatusesAsync(JiraConnection c, IEnumerable<string> keys, CancellationToken ct = default)
    {
        string Key(string issue) => $"{c.BaseUrl}|{issue.ToUpperInvariant()}";
        var wanted = keys.Where(k => k != "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var missing = wanted.Where(k => !_statuses.TryGet(Key(k), out _)).ToList();
        if (missing.Count > 0)
        {
            var found = (await jira.GetIssuesAsync(c, missing, ct)).ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var k in missing) _statuses.Set(Key(k), found.GetValueOrDefault(k));
        }
        return wanted
            .Select(k => _statuses.TryGet(Key(k), out var issue) ? issue : null)
            .OfType<IssueWithStatus>()
            .ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
    }
}
