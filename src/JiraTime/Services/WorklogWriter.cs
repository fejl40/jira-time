using System.Net;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// Writes one entry's worklog to Jira, or deletes it. What Jira can't take waits in the retry queue
/// with the reason; once Jira has it, it leaves the queue. Used by <see cref="WorklogSync"/>.
/// </summary>
public class WorklogWriter(JiraWorklogClient jira, RetryQueue queue, TimeLog log)
{
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
            if (entry.MovedToAnotherIssue) await RemoveFromOldIssueAsync(c, entry, ct);
            await SendAsync(c, entry, ct);
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

    /// <summary>
    /// Removes the work: its worklog is deleted in Jira. If that fails the removal is queued (the work is
    /// hidden from totals) and retried by <see cref="WorklogSync.SyncRangeAsync"/>.
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

    /// <summary>Worklogs can't move, so the one on the old issue is removed and a new one created.</summary>
    private async Task RemoveFromOldIssueAsync(JiraConnection c, TimeEntry entry, CancellationToken ct)
    {
        var oldIssue = entry.WorklogIssueKey;
        await DeleteIgnoringMissingAsync(c, oldIssue, entry.JiraWorklogId!, ct);
        log.Written(new JiraIssue(oldIssue, ""));
        entry.JiraWorklogId = null;
        entry.JiraWorklogIssueKey = null;
    }

    private async Task SendAsync(JiraConnection c, TimeEntry entry, CancellationToken ct)
    {
        if (entry.JiraWorklogId is null)
        {
            entry.JiraWorklogId = await jira.AddWorklogAsync(c, entry, ct);
            return;
        }
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

    private async Task<string?> FinishDeleteAsync(JiraConnection c, TimeEntry entry, CancellationToken ct)
    {
        var issue = entry.WorklogIssueKey;
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
