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
