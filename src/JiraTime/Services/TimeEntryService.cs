using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

public class TimeEntryService(IDocumentStore store)
{
    private readonly IDocumentCollection<TimeEntry> _docs = store.Collection<TimeEntry>();

    public async Task<IReadOnlyList<TimeEntry>> GetRangeAsync(DateOnly from, DateOnly to)
    {
        var entries = await _docs.QueryAsync(e => e.Date >= from && e.Date <= to);
        return entries.OrderBy(e => e.Date).ThenBy(e => e.Start).ToList();
    }

    /// <summary>Most recently used issues, newest first. Used as quick picks when adding an entry.</summary>
    public async Task<IReadOnlyList<JiraIssue>> GetRecentIssuesAsync(int count = 8)
    {
        var since = DateOnly.FromDateTime(DateTime.Today.AddDays(-60));
        var entries = await _docs.QueryAsync(e => e.Date >= since);
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
        if (entry.End <= entry.Start) return "End time must be after start time.";

        var sameDay = await _docs.QueryAsync(e => e.Date == entry.Date && e.Id != entry.Id);
        var clash = sameDay.FirstOrDefault(e => entry.Start < e.End && e.Start < entry.End);
        if (clash is not null)
            return $"Overlaps {clash.IssueKey} {clash.Start:HH\\:mm}–{clash.End:HH\\:mm}.";

        entry.UpdatedUtc = DateTime.UtcNow;
        entry.SyncPending = true;
        await _docs.UpsertAsync(entry);
        return null;
    }

    public Task<TimeEntry?> GetAsync(string id) => _docs.GetAsync(id);

    /// <summary>Stores the result of a Jira sync. No validation and no change to <see cref="TimeEntry.UpdatedUtc"/>.</summary>
    public Task SaveSyncStateAsync(TimeEntry entry) => _docs.UpsertAsync(entry);

    /// <summary>Entries in the range that Jira does not have (yet) in their current form.</summary>
    public async Task<IReadOnlyList<TimeEntry>> GetUnsyncedAsync(DateOnly from, DateOnly to) =>
        (await GetRangeAsync(from, to)).Where(e => !e.IsSynced).ToList();

    public Task DeleteAsync(string id) => _docs.DeleteAsync(id);
}
