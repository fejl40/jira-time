using System.Net;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// Writes work to Jira. Jira is where logged time lives: saving a day creates, updates and deletes its
/// worklogs directly. When Jira can't be reached the change is kept in the local retry queue
/// (<see cref="RetryQueue"/>), shown as not in Jira with the reason, and sent by <see cref="SyncRangeAsync"/>.
/// </summary>
public class WorklogSync(JiraWorklogClient jira, RetryQueue queue, TimeLog log)
{
    /// <summary>Outcome of <see cref="SaveDayAsync"/>.</summary>
    /// <param name="Errors">Row id → why it is invalid; <see cref="DayError"/> for the day as a whole. Empty when saved.</param>
    /// <param name="SyncFailures">Changes Jira did not get (yet); they wait in the retry queue.</param>
    /// <param name="ChangedInJira">Nothing was saved because Jira's worklogs changed since the day was read.</param>
    public record DayResult(IReadOnlyDictionary<string, string> Errors, int SyncFailures, bool ChangedInJira = false)
    {
        public const string DayError = "";
        public bool Saved => Errors.Count == 0;
    }

    public const string ChangedInJiraMessage =
        "This day's worklogs were changed in Jira since you opened it, so nothing was saved. The day now shows Jira's version with your changes on top; check them and save again.";

    /// <summary>
    /// Makes the day's work match <paramref name="rows"/>: new rows are created in Jira, changed rows
    /// updated, and work in <paramref name="saved"/> missing from the rows deleted. Every row is validated
    /// first; if any is invalid, or a worklog to change was changed in Jira since it was read, nothing is written.
    /// </summary>
    /// <param name="saved">The day's work as it was read, which the rows were edited from.</param>
    public async Task<DayResult> SaveDayAsync(JiraConnection c, DateOnly date, IReadOnlyList<TimeEntry> rows, IReadOnlyList<TimeEntry> saved,
        CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();
        foreach (var r in rows)
        {
            r.Date = date;
            if (TimeEntry.Validate(r) is { } error) errors[r.Id] = error;
        }
        if (rows.Aggregate(TimeSpan.Zero, (t, r) => t + r.Duration) > TimeEntry.MaxDuration)
            errors[DayResult.DayError] = TimeEntry.TooManyHours;
        if (errors.Count > 0) return new DayResult(errors, 0);

        var before = saved.ToDictionary(e => e.Id);
        var gone = saved.Where(e => rows.All(r => r.Id != e.Id)).ToList();
        var changed = rows.Where(r => before.TryGetValue(r.Id, out var old) && !r.SameAsSynced(old)).ToList();

        var touched = gone.Concat(changed.Select(r => before[r.Id])).Where(e => e.IsSynced).ToList();
        if (await ChangedInJiraAsync(c, touched, ct))
        {
            log.Refresh();
            return new DayResult(new Dictionary<string, string> { [DayResult.DayError] = ChangedInJiraMessage }, 0, ChangedInJira: true);
        }

        var failures = 0;
        foreach (var e in gone)
            if (await DeleteAsync(c, e, ct) is not null) failures++;

        foreach (var row in rows)
        {
            if (before.TryGetValue(row.Id, out var old))
            {
                if (row.SameAsSynced(old)) continue;
                // Jira bookkeeping comes from what was read, not from the edited copy.
                row.JiraWorklogId = old.JiraWorklogId;
                row.JiraWorklogIssueKey = old.JiraWorklogIssueKey;
                row.CreatedUtc = old.CreatedUtc;
            }
            row.UpdatedUtc = DateTime.UtcNow;
            if (await PushAsync(c, row, ct) is not null) failures++;
        }
        return new DayResult(errors, failures);
    }

    /// <summary>
    /// True when one of the worklogs is gone from Jira or no longer has the day, hours and comment it was
    /// read with. When Jira can't be asked the save goes ahead: its changes then wait in the retry queue.
    /// </summary>
    private async Task<bool> ChangedInJiraAsync(JiraConnection c, IReadOnlyList<TimeEntry> read, CancellationToken ct)
    {
        try
        {
            foreach (var e in read)
            {
                var now = await jira.GetWorklogAsync(c, e.JiraWorklogIssueKey ?? e.IssueKey, e.JiraWorklogId!, ct);
                if (now is null || !e.Matches(now, includeIssue: false)) return true;
            }
        }
        catch (JiraException)
        {
            return false;
        }
        return false;
    }

    /// <summary>
    /// Sends the entry's current state to Jira. When Jira has it, any queued copy is dropped; otherwise the
    /// entry is queued with the reason.
    /// </summary>
    /// <returns>The error message, or null when Jira is up to date.</returns>
    public async Task<string?> PushAsync(JiraConnection c, TimeEntry entry, CancellationToken ct = default)
    {
        if (entry.DeletePending) return await FinishDeleteAsync(c, entry, ct);
        try
        {
            // Moved to another issue: worklogs can't move, so remove the old one and create a new one.
            if (entry.JiraWorklogId is not null && !string.Equals(entry.JiraWorklogIssueKey, entry.IssueKey, StringComparison.OrdinalIgnoreCase))
            {
                var oldIssue = entry.JiraWorklogIssueKey ?? entry.IssueKey;
                await DeleteIgnoringMissingAsync(c, oldIssue, entry.JiraWorklogId, ct);
                log.Written(new JiraIssue(oldIssue, ""));
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
        }
        catch (JiraException ex)
        {
            entry.SyncPending = true;
            entry.SyncError = ex.Message;
            await QueueAsync(entry);
            return entry.SyncError;
        }

        entry.JiraWorklogIssueKey = entry.IssueKey;
        entry.SyncPending = false;
        entry.SyncError = null;
        log.Written(new JiraIssue(entry.IssueKey, entry.IssueSummary));
        await queue.RemoveAsync(entry.Id);
        return null;
    }

    /// <summary>Sends every change in the range that waits in the retry queue.</summary>
    /// <returns>How many are still waiting afterwards.</returns>
    public async Task<int> SyncRangeAsync(JiraConnection c, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var failed = 0;
        foreach (var e in await queue.GetPendingAsync(from, to))
            if (await PushAsync(c, e, ct) is not null) failed++;
        return failed;
    }

    /// <summary>
    /// Removes the work: its worklog is deleted in Jira. If that fails the removal is queued (the work is
    /// hidden from totals) and retried by <see cref="SyncRangeAsync"/>.
    /// </summary>
    /// <returns>Null when Jira is up to date, otherwise why the Jira delete has to be retried.</returns>
    public async Task<string?> DeleteAsync(JiraConnection c, TimeEntry entry, CancellationToken ct = default)
    {
        if (entry.JiraWorklogId is null)
        {
            await queue.RemoveAsync(entry.Id); // work that never reached Jira
            return null;
        }
        entry.DeletePending = true;
        entry.SyncPending = true;
        return await FinishDeleteAsync(c, entry, ct);
    }

    private async Task<string?> FinishDeleteAsync(JiraConnection c, TimeEntry entry, CancellationToken ct)
    {
        var issue = entry.JiraWorklogIssueKey ?? entry.IssueKey;
        try
        {
            if (entry.JiraWorklogId is not null)
                await DeleteIgnoringMissingAsync(c, issue, entry.JiraWorklogId, ct);
        }
        catch (JiraException ex)
        {
            entry.SyncError = ex.Message;
            await QueueAsync(entry);
            return ex.Message;
        }
        log.Written(new JiraIssue(issue, entry.IssueSummary));
        await queue.RemoveAsync(entry.Id);
        return null;
    }

    /// <summary>
    /// Keeps the change in the retry queue. Work read from Jira gets an id of its own there, as worklog
    /// ids are only unique within one Jira.
    /// </summary>
    private async Task QueueAsync(TimeEntry entry)
    {
        if (entry.Id.StartsWith(WorklogMatching.JiraIdPrefix, StringComparison.Ordinal)) entry.Id = Guid.NewGuid().ToString("N");
        await queue.AddAsync(entry);
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
