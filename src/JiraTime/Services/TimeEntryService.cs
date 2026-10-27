using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>The signed-in user's time entries. Every read and write is limited to <see cref="CurrentUser"/>.</summary>
public class TimeEntryService(IDocumentStore store, CurrentUser user)
{
    private readonly IDocumentCollection<TimeEntry> _docs = store.Collection<TimeEntry>();

    public async Task<IReadOnlyList<TimeEntry>> GetRangeAsync(DateOnly from, DateOnly to)
    {
        var owner = user.Owner;
        var entries = await _docs.QueryAsync(e => e.Owner == owner && e.Date >= from && e.Date <= to && !e.DeletePending);
        return entries.OrderBy(e => e.Date).ThenBy(e => e.CreatedUtc).ToList();
    }

    public const string TooManyHours = "A day can't have more than 24 hours.";

    /// <summary>Rules for a single entry, without looking at the rest of its day.</summary>
    /// <returns>An error message, or null when the entry is fine.</returns>
    public static string? Validate(TimeEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.IssueKey)) return "Pick a Jira issue.";
        if (entry.Duration <= TimeSpan.Zero) return "Enter the hours, e.g. 1.5.";
        if (entry.Duration.Ticks % TimeSpan.TicksPerMinute != 0) return "Hours must be whole minutes.";
        if (entry.Duration > TimeEntry.MaxDuration) return TooManyHours;
        return null;
    }

    /// <param name="checkDayTotal">False when the caller already checked the whole day (saving a day at once).</param>
    /// <returns>An error message, or null when saved.</returns>
    public async Task<string?> SaveAsync(TimeEntry entry, bool checkDayTotal = true)
    {
        if (Validate(entry) is { } error) return error;
        var owner = user.Owner;
        if (!await IsMineOrNewAsync(entry)) return "This entry belongs to someone else.";
        entry.Owner = owner;

        if (checkDayTotal)
        {
            var sameDay = await _docs.QueryAsync(e => e.Owner == owner && e.Date == entry.Date && e.Id != entry.Id && !e.DeletePending);
            if (sameDay.Aggregate(entry.Duration, (t, e) => t + e.Duration) > TimeEntry.MaxDuration) return TooManyHours;
        }

        entry.UpdatedUtc = DateTime.UtcNow;
        entry.SyncPending = true;
        await _docs.UpsertAsync(entry);
        return null;
    }

    /// <returns>The entry, or null when it doesn't exist or belongs to someone else.</returns>
    public async Task<TimeEntry?> GetAsync(string id) =>
        await _docs.GetAsync(id) is { } e && e.Owner == user.Owner ? e : null;

    /// <summary>Stores the result of a Jira sync. No validation and no change to <see cref="TimeEntry.UpdatedUtc"/>.</summary>
    public async Task SaveSyncStateAsync(TimeEntry entry)
    {
        if (!await IsMineOrNewAsync(entry)) throw new InvalidOperationException("This entry belongs to someone else.");
        entry.Owner = user.Owner;
        await _docs.UpsertAsync(entry);
    }

    private async Task<bool> IsMineOrNewAsync(TimeEntry entry) =>
        await _docs.GetAsync(entry.Id) is not { } stored || stored.Owner == user.Owner;

    /// <summary>
    /// Entries in the range that Jira does not have (yet) in their current form, including removed
    /// work whose worklog still has to be deleted.
    /// </summary>
    public async Task<IReadOnlyList<TimeEntry>> GetUnsyncedAsync(DateOnly from, DateOnly to) =>
        (await QueryMineAsync(from, to)).Where(e => !e.IsSynced).OrderBy(e => e.Date).ToList();

    /// <summary>Work in the range that Jira doesn't have yet: new or changed work, and removals still to send.</summary>
    public async Task<IReadOnlyList<TimeEntry>> GetPendingAsync(DateOnly from, DateOnly to) =>
        (await QueryMineAsync(from, to)).Where(e => !e.IsSynced).OrderBy(e => e.Date).ThenBy(e => e.CreatedUtc).ToList();

    /// <summary>Every entry in the range, including removed work whose worklog delete is still pending.</summary>
    public Task<IReadOnlyList<TimeEntry>> GetRangeWithRemovedAsync(DateOnly from, DateOnly to) => QueryMineAsync(from, to);

    /// <summary>The user's entries linked to any of these Jira worklogs, on any day.</summary>
    public async Task<IReadOnlyList<TimeEntry>> GetByWorklogIdsAsync(IReadOnlyCollection<string> worklogIds)
    {
        if (worklogIds.Count == 0) return [];
        var owner = user.Owner;
        var ids = worklogIds.ToHashSet();
        return (await _docs.QueryAsync(e => e.Owner == owner && e.JiraWorklogId != null))
            .Where(e => ids.Contains(e.JiraWorklogId!)).ToList();
    }

    private Task<IReadOnlyList<TimeEntry>> QueryMineAsync(DateOnly from, DateOnly to)
    {
        var owner = user.Owner;
        return _docs.QueryAsync(e => e.Owner == owner && e.Date >= from && e.Date <= to);
    }

    /// <summary>Deletes the entry if it is the signed-in user's.</summary>
    public async Task DeleteAsync(string id)
    {
        if (await GetAsync(id) is not null) await _docs.DeleteAsync(id);
    }
}
