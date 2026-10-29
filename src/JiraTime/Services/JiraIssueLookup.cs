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
    private readonly ExpiringCache<string?> _epics = new(clock, CacheFor);
    private readonly ExpiringCache<string?> _epicLinkFields = new(clock, TimeSpan.FromHours(12));

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

    /// <summary>
    /// The epic of each issue Jira knows (null when it has none), looked up once per key while cached.
    /// Throws <see cref="JiraException"/> when Jira can't be read.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string?>> GetEpicsAsync(JiraConnection c, IEnumerable<string> keys, CancellationToken ct = default)
    {
        string Key(string issue) => $"{c.BaseUrl}|{issue.ToUpperInvariant()}";
        var wanted = keys.Where(k => k != "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var missing = wanted.Where(k => !_epics.TryGet(Key(k), out _)).ToList();
        if (missing.Count > 0)
        {
            var field = await _epicLinkFields.GetOrLoadAsync(c.BaseUrl, () => jira.GetEpicLinkFieldAsync(c, ct));
            var found = await jira.GetEpicsAsync(c, missing, field, ct);
            foreach (var k in missing) _epics.Set(Key(k), found.GetValueOrDefault(k));
        }
        return wanted.ToDictionary(k => k, k => _epics.TryGet(Key(k), out var epic) ? epic : null, StringComparer.OrdinalIgnoreCase);
    }
}
