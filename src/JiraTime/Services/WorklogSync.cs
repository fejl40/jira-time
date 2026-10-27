using System.Net;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// Keeps Jira worklogs in step with local time entries. Every save, edit and delete goes to
/// Jira right away; when Jira can't be reached the entry stays saved locally and is marked
/// as not synced, with the reason, so it can be retried.
/// </summary>
public class WorklogSync(JiraClient jira, TimeEntryService entries)
{
    public record SaveResult(string? ValidationError, string? SyncError)
    {
        public bool Saved => ValidationError is null;
    }

    /// <summary>Outcome of <see cref="SaveDayAsync"/>.</summary>
    /// <param name="Errors">Row id → why it is invalid; <see cref="DayError"/> for the day as a whole. Empty when saved.</param>
    /// <param name="SyncFailures">Rows saved here that Jira did not get (yet).</param>
    public record DayResult(IReadOnlyDictionary<string, string> Errors, int SyncFailures)
    {
        public const string DayError = "";
        public bool Saved => Errors.Count == 0;
    }

    /// <summary>
    /// Makes the day's work match <paramref name="rows"/>: new rows are created, changed rows updated,
    /// and saved entries missing from the rows removed, each in Jira too. Every row is validated
    /// first; if any is invalid nothing is written.
    /// </summary>
    public async Task<DayResult> SaveDayAsync(JiraConnection c, DateOnly date, IReadOnlyList<TimeEntry> rows, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();
        foreach (var r in rows)
        {
            r.Date = date;
            if (TimeEntryService.Validate(r) is { } error) errors[r.Id] = error;
        }
        if (rows.Aggregate(TimeSpan.Zero, (t, r) => t + r.Duration) > TimeEntry.MaxDuration)
            errors[DayResult.DayError] = TimeEntryService.TooManyHours;
        if (errors.Count > 0) return new DayResult(errors, 0);

        var saved = (await entries.GetRangeAsync(date, date)).ToDictionary(e => e.Id);
        var failures = 0;

        foreach (var gone in saved.Values.Where(e => rows.All(r => r.Id != e.Id)))
            if (await DeleteAsync(c, gone, ct) is not null) failures++;

        foreach (var row in rows)
        {
            if (saved.TryGetValue(row.Id, out var old))
            {
                if (row.SameAsSynced(old)) continue;
                // Jira bookkeeping comes from what is stored, not from the edited copy.
                row.JiraWorklogId = old.JiraWorklogId;
                row.JiraWorklogIssueKey = old.JiraWorklogIssueKey;
                row.CreatedUtc = old.CreatedUtc;
            }
            await entries.SaveAsync(row, checkDayTotal: false);
            if (await SyncAsync(c, row, ct) is not null) failures++;
        }
        return new DayResult(errors, failures);
    }

    /// <summary>Validates and saves the entry locally, then sends it to Jira.</summary>
    public async Task<SaveResult> SaveAsync(JiraConnection c, TimeEntry entry, CancellationToken ct = default)
    {
        var error = await entries.SaveAsync(entry);
        if (error is not null) return new SaveResult(error, null);
        return new SaveResult(null, await SyncAsync(c, entry, ct));
    }

    /// <summary>Pushes the entry's current state to Jira and records the outcome on the entry.</summary>
    /// <returns>The error message, or null when Jira is up to date.</returns>
    public async Task<string?> SyncAsync(JiraConnection c, TimeEntry entry, CancellationToken ct = default)
    {
        if (entry.DeletePending) return await FinishDeleteAsync(c, entry, ct);
        try
        {
            // Moved to another issue: worklogs can't move, so remove the old one and create a new one.
            if (entry.JiraWorklogId is not null && !string.Equals(entry.JiraWorklogIssueKey, entry.IssueKey, StringComparison.OrdinalIgnoreCase))
            {
                await DeleteIgnoringMissingAsync(c, entry.JiraWorklogIssueKey ?? entry.IssueKey, entry.JiraWorklogId, ct);
                entry.JiraWorklogId = null;
                entry.JiraWorklogIssueKey = null;
            }

            if (entry.JiraWorklogId is null)
            {
                entry.JiraWorklogId = await jira.AddWorklogAsync(c, entry, ct);
            }
            else
            {
                try
                {
                    await jira.UpdateWorklogAsync(c, entry.IssueKey, entry.JiraWorklogId, entry, ct);
                }
                catch (JiraException ex) when (ex.Status == HttpStatusCode.NotFound)
                {
                    // Someone deleted it in Jira: log it again.
                    entry.JiraWorklogId = await jira.AddWorklogAsync(c, entry, ct);
                }
            }

            entry.JiraWorklogIssueKey = entry.IssueKey;
            entry.SyncPending = false;
            entry.SyncError = null;
        }
        catch (JiraException ex)
        {
            entry.SyncPending = true;
            entry.SyncError = ex.Message;
        }

        await entries.SaveSyncStateAsync(entry);
        return entry.SyncError;
    }

    /// <summary>What <see cref="ImportAsync"/> changed locally.</summary>
    public record ImportResult(int Added, int Updated, int Removed)
    {
        public int Changes => Added + Updated + Removed;
    }

    /// <summary>
    /// Brings the user's worklogs in Jira for the range into the timesheet. Worklogs logged elsewhere
    /// are added (marked <see cref="TimeEntry.FromJira"/>); entries Jira already has take Jira's issue,
    /// day, hours and comment; entries whose worklog is gone from Jira are removed. Entries with
    /// local changes Jira doesn't have yet are left alone: they are sent to Jira instead.
    /// </summary>
    public async Task<ImportResult> ImportAsync(JiraConnection c, JiraProfile profile, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var remote = await jira.GetMyWorklogsAsync(c, profile, from, to, ct);
        var remoteIds = remote.Select(w => w.Id).ToHashSet();
        var inRange = await entries.GetRangeWithRemovedAsync(from, to);
        var linked = await LinkedEntriesAsync(inRange, remoteIds);

        var changes = new List<ImportChange>();
        foreach (var w in remote)
            changes.Add(await ApplyRemoteAsync(w, linked));
        foreach (var e in inRange.Where(e => e.IsSynced && !e.DeletePending && !remoteIds.Contains(e.JiraWorklogId!)))
            changes.Add(await ReconcileMissedAsync(c, e, ct));

        return new ImportResult(
            changes.Count(x => x == ImportChange.Added),
            changes.Count(x => x == ImportChange.Updated),
            changes.Count(x => x == ImportChange.Removed));
    }

    private enum ImportChange { None, Added, Updated, Removed }

    /// <summary>Entries linked to a worklog, by worklog id: those in the range plus any the search found on other days.</summary>
    private async Task<Dictionary<string, TimeEntry>> LinkedEntriesAsync(IReadOnlyList<TimeEntry> inRange, IReadOnlyCollection<string> remoteIds) =>
        inRange.Concat(await entries.GetByWorklogIdsAsync(remoteIds))
            .Where(e => e.JiraWorklogId is not null)
            .DistinctBy(e => e.Id)
            .GroupBy(e => e.JiraWorklogId!)
            .ToDictionary(g => g.Key, g => g.First());

    /// <summary>Updates the entry linked to a worklog the search found, or adds one when none is linked.</summary>
    private async Task<ImportChange> ApplyRemoteAsync(JiraWorklog w, Dictionary<string, TimeEntry> linked)
    {
        if (!linked.TryGetValue(w.Id, out var e))
        {
            await entries.SaveSyncStateAsync(w.ToEntry());
            return ImportChange.Added;
        }
        if (e.SyncPending || e.DeletePending) return ImportChange.None; // ours is newer; Sync sends it
        if (e.Matches(w)) return ImportChange.None;
        e.TakeFrom(w);
        await entries.SaveSyncStateAsync(e);
        return ImportChange.Updated;
    }

    /// <summary>
    /// A synced entry the search didn't find: deleted in Jira, moved to another day there, or Jira's search
    /// index hasn't caught up yet. Asks for the worklog itself before dropping anything.
    /// </summary>
    private async Task<ImportChange> ReconcileMissedAsync(JiraConnection c, TimeEntry e, CancellationToken ct)
    {
        var w = await jira.GetWorklogAsync(c, e.JiraWorklogIssueKey ?? e.IssueKey, e.JiraWorklogId!, ct);
        if (w is null)
        {
            await entries.DeleteAsync(e.Id);
            return ImportChange.Removed;
        }
        // A single worklog comes without the issue summary, so the issue is left as it is.
        if (e.Matches(w, includeIssue: false)) return ImportChange.None;
        e.TakeFrom(w, includeIssue: false);
        await entries.SaveSyncStateAsync(e);
        return ImportChange.Updated;
    }

    /// <summary>Retries every entry in the range that Jira doesn't have yet.</summary>
    /// <returns>How many entries are still not synced afterwards.</returns>
    public async Task<int> SyncRangeAsync(JiraConnection c, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var failed = 0;
        foreach (var e in await entries.GetUnsyncedAsync(from, to))
            if (await SyncAsync(c, e, ct) is not null) failed++;
        return failed;
    }

    /// <summary>
    /// Removes the work. Its worklog is deleted in Jira; if that fails the entry is kept as a pending
    /// removal (hidden from totals) and the delete is retried by <see cref="SyncAsync"/>.
    /// </summary>
    /// <returns>Null when Jira is up to date, otherwise why the Jira delete has to be retried.</returns>
    public async Task<string?> DeleteAsync(JiraConnection c, TimeEntry entry, CancellationToken ct = default)
    {
        if (entry.JiraWorklogId is null)
        {
            await entries.DeleteAsync(entry.Id);
            return null;
        }
        entry.DeletePending = true;
        entry.SyncPending = true;
        await entries.SaveSyncStateAsync(entry);
        return await FinishDeleteAsync(c, entry, ct);
    }

    private async Task<string?> FinishDeleteAsync(JiraConnection c, TimeEntry entry, CancellationToken ct)
    {
        try
        {
            if (entry.JiraWorklogId is not null)
                await DeleteIgnoringMissingAsync(c, entry.JiraWorklogIssueKey ?? entry.IssueKey, entry.JiraWorklogId, ct);
        }
        catch (JiraException ex)
        {
            entry.SyncError = ex.Message;
            await entries.SaveSyncStateAsync(entry);
            return ex.Message;
        }
        await entries.DeleteAsync(entry.Id);
        return null;
    }

    private async Task DeleteIgnoringMissingAsync(JiraConnection c, string issueKey, string worklogId, CancellationToken ct)
    {
        try
        {
            await jira.DeleteWorklogAsync(c, issueKey, worklogId, ct);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            // Already gone.
        }
    }
}
