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
        var statuses = await PerKeyAsync(_statuses, c, keys, async missing =>
            (await jira.GetIssuesAsync(c, missing, ct)).ToDictionary(i => i.Key, i => (IssueWithStatus?)i, StringComparer.OrdinalIgnoreCase));
        return statuses.Select(s => s.Value).OfType<IssueWithStatus>().ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The epic of each issue Jira knows (null when it has none), looked up once per key while cached.
    /// Throws <see cref="JiraException"/> when Jira can't be read.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string?>> GetEpicsAsync(JiraConnection c, IEnumerable<string> keys, CancellationToken ct = default)
    {
        var epics = await PerKeyAsync(_epics, c, keys, async missing =>
        {
            var field = await _epicLinkFields.GetOrLoadAsync(c.BaseUrl, () => jira.GetEpicLinkFieldAsync(c, ct));
            return await jira.GetEpicsAsync(c, missing, field, ct);
        });
        return epics.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The cached value of each issue key, after loading the keys not cached yet in one go. A key the load
    /// leaves out is cached as null, so it isn't asked again while cached.
    /// </summary>
    private static async Task<IReadOnlyList<(string Key, T? Value)>> PerKeyAsync<T>(ExpiringCache<T?> cache, JiraConnection c,
        IEnumerable<string> keys, Func<IReadOnlyCollection<string>, Task<IReadOnlyDictionary<string, T?>>> load) where T : class
    {
        string CacheKey(string issue) => $"{c.BaseUrl}|{issue.ToUpperInvariant()}";
        var wanted = keys.Where(k => k != "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var missing = wanted.Where(k => !cache.TryGet(CacheKey(k), out _)).ToList();
        if (missing.Count > 0)
        {
            var found = await load(missing);
            foreach (var k in missing) cache.Set(CacheKey(k), found.GetValueOrDefault(k));
        }
        return wanted.Select(k => (k, cache.TryGet(CacheKey(k), out var value) ? value : null)).ToList();
    }
}
