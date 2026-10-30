using JiraTime.Domain;
using JiraTime.Services.Jira;
using JiraTime.Services.SignIn;

namespace JiraTime.Services.Worklogs;

/// <summary>Logged time for a range of days: Jira's worklogs with the work Jira doesn't have yet laid over them.</summary>
/// <param name="JiraError">Why Jira couldn't be read; then <see cref="Entries"/> holds only the work waiting for Jira.</param>
public sealed record LoggedTime(IReadOnlyList<TimeEntry> Entries, string? JiraError)
{
    /// <summary>True when Jira was read, so the entries are all the time logged.</summary>
    public bool IsComplete => JiraError is null;

    public IReadOnlyList<TimeEntry> Between(DateOnly from, DateOnly to) => Entries.Where(e => e.Date >= from && e.Date <= to).ToList();
}

/// <summary>
/// The signed-in user's logged time. Jira is the source of truth: work is read from the user's Jira
/// worklogs, however they were logged (here, in Jira, in WorklogPRO). The only time kept locally is work
/// Jira didn't get yet (<see cref="RetryQueue"/>), shown on top until it is sent.
/// </summary>
public class TimeLog(JiraWorklogClient jira, RetryQueue pending, CurrentUser user, RecentWorklogIssues recent, TimeProvider clock)
{
    /// <summary>How long worklogs read from Jira are reused before reading them again.</summary>
    public static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(1);

    private readonly ExpiringCache<IReadOnlyList<JiraWorklog>> _cache = new(clock, CacheLifetime);

    public async Task<LoggedTime> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var waiting = await pending.GetPendingAsync(from, to);
        if (user.Jira is not { } signedIn)
            return new LoggedTime(WorklogMatching.Merge([], waiting), "Not connected to Jira.");
        try
        {
            var worklogs = await _cache.GetOrLoadAsync($"{from:O}|{to:O}",
                () => jira.GetMyWorklogsAsync(signedIn.Connection, signedIn.Profile, from, to, recent.For(user.Owner), ct));
            return new LoggedTime(WorklogMatching.Merge(worklogs, waiting), null);
        }
        catch (JiraException ex)
        {
            return new LoggedTime(WorklogMatching.Merge([], waiting), ex.Message);
        }
    }

    /// <summary>Work from the last <see cref="RecentWork.Days"/> days, for recent issues and comments. Empty when Jira can't be read.</summary>
    public async Task<IReadOnlyList<TimeEntry>> GetRecentAsync(CancellationToken ct = default)
    {
        var today = clock.Today(user.TimeZone);
        return (await GetRangeAsync(today.AddDays(-RecentWork.Days), today, ct)).Entries;
    }

    /// <summary>A worklog was written on the issue: read Jira again, and read the issue directly until its search catches up.</summary>
    public void Written(JiraIssue issue)
    {
        recent.Add(user.Owner, issue);
        Refresh();
    }

    /// <summary>Forgets what was read, so the next read asks Jira again.</summary>
    public void Refresh() => _cache.Clear();
}
