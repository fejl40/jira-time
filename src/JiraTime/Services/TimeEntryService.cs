using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

public class TimeEntryService(IDocumentStore store)
{
    private readonly IDocumentCollection<TimeEntry> _docs = store.Collection<TimeEntry>();

    public async Task<IReadOnlyList<TimeEntry>> GetRangeAsync(DateOnly from, DateOnly to)
    {
        var entries = await _docs.QueryAsync(e => e.Date >= from && e.Date <= to && !e.DeletePending);
        return entries.OrderBy(e => e.Date).ThenBy(e => e.CreatedUtc).ToList();
    }

    /// <summary>Most recently used issues, newest first. Used as quick picks when adding an entry.</summary>
    public async Task<IReadOnlyList<JiraIssue>> GetRecentIssuesAsync(int count = 8)
    {
        var since = DateOnly.FromDateTime(DateTime.Today.AddDays(-60));
        var entries = await _docs.QueryAsync(e => e.Date >= since && !e.DeletePending);
        return entries
            .OrderByDescending(e => e.UpdatedUtc)
            .DistinctBy(e => e.IssueKey)
            .Take(count)
            .Select(e => new JiraIssue(e.IssueKey, e.IssueSummary))
            .ToList();
    }

    /// <returns>An error message, or null when saved.</returns>
    public async Task<string?> SaveAsync(TimeEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.IssueKey)) return "Pick a Jira issue.";
        if (entry.Duration <= TimeSpan.Zero) return "Enter the hours, e.g. 1.5.";
        if (entry.Duration.Ticks % TimeSpan.TicksPerMinute != 0) return "Hours must be whole minutes.";

        var sameDay = await _docs.QueryAsync(e => e.Date == entry.Date && e.Id != entry.Id && !e.DeletePending);
        var dayTotal = sameDay.Aggregate(entry.Duration, (t, e) => t + e.Duration);
        if (dayTotal > TimeEntry.MaxDuration) return "A day can't have more than 24 hours.";

        entry.UpdatedUtc = DateTime.UtcNow;
        entry.SyncPending = true;
        await _docs.UpsertAsync(entry);
        return null;
    }

    public Task<TimeEntry?> GetAsync(string id) => _docs.GetAsync(id);

    /// <summary>Stores the result of a Jira sync. No validation and no change to <see cref="TimeEntry.UpdatedUtc"/>.</summary>
    public Task SaveSyncStateAsync(TimeEntry entry) => _docs.UpsertAsync(entry);

    /// <summary>
    /// Entries in the range that Jira does not have (yet) in their current form, including removed
    /// work whose worklog still has to be deleted.
    /// </summary>
    public async Task<IReadOnlyList<TimeEntry>> GetUnsyncedAsync(DateOnly from, DateOnly to) =>
        (await _docs.QueryAsync(e => e.Date >= from && e.Date <= to)).Where(e => !e.IsSynced).OrderBy(e => e.Date).ToList();

    public Task DeleteAsync(string id) => _docs.DeleteAsync(id);
}
