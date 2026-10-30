using System.Text.Json.Serialization;
using JiraTime.Storage;

namespace JiraTime.Domain;

/// <summary>One block of work on one Jira issue, on one day.</summary>
[DocumentCollection("time-entries")]
public class TimeEntry : IOwnedDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary><see cref="UserKey.Id"/> of the user. Empty on data from before multi-user support.</summary>
    public string? Owner { get; set; }

    public required string IssueKey { get; set; }
    /// <summary>Cached so the timesheet renders without calling Jira.</summary>
    public string IssueSummary { get; set; } = "";

    public DateOnly Date { get; set; }

    /// <summary>Hours worked on the issue that day. When during the day does not matter.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>The worklog comment. Stored as "description", its name before it was called a comment.</summary>
    [JsonPropertyName("description")]
    public string? Comment { get; set; }

    /// <summary>Time of day put on the Jira worklog, which needs one.</summary>
    public static readonly TimeOnly JiraStartTime = new(9, 0);

    /// <summary>Most hours one entry, or one day, can have.</summary>
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);

    // The sync state below changes only through the transitions further down (Sent, Failed, ...).
    private string? _worklogId, _worklogIssueKey, _syncError;
    private bool _syncPending = true, _deletePending;

    /// <summary>Set once the entry has been pushed to Jira as a worklog.</summary>
    public string? JiraWorklogId { get => _worklogId; init => _worklogId = value; }

    /// <summary>The issue the worklog lives on. Differs from <see cref="IssueKey"/> after the entry moved to another issue.</summary>
    public string? JiraWorklogIssueKey { get => _worklogIssueKey; init => _worklogIssueKey = value; }

    /// <summary>True when the entry changed locally and Jira has not caught up yet.</summary>
    public bool SyncPending { get => _syncPending; init => _syncPending = value; }

    /// <summary>
    /// The user removed this work, but Jira still has its worklog. Hidden everywhere except
    /// "not in Jira"; the worklog delete is retried until it succeeds, then the entry is dropped.
    /// </summary>
    public bool DeletePending { get => _deletePending; init => _deletePending = value; }

    /// <summary>Why the last attempt to sync failed, if it did.</summary>
    public string? SyncError { get => _syncError; init => _syncError = value; }

    /// <summary>When the entry was first saved; the default until then.</summary>
    public DateTime CreatedUtc { get; set; }
    /// <summary>When the entry was last saved; the default until then.</summary>
    public DateTime UpdatedUtc { get; set; }

    public const string TooManyHours = "A day can't have more than 24 hours.";

    /// <summary>Rules for a single entry, without looking at the rest of its day.</summary>
    /// <returns>An error message, or null when the entry is fine.</returns>
    public static string? Validate(TimeEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.IssueKey)) return "Pick a Jira issue.";
        if (entry.Duration <= TimeSpan.Zero) return "Enter the hours, e.g. 1.5.";
        if (entry.Duration.Ticks % TimeSpan.TicksPerMinute != 0) return "Hours must be whole minutes.";
        if (entry.Duration > MaxDuration) return TooManyHours;
        return null;
    }

    /// <summary>A separate copy to edit without touching this one.</summary>
    public TimeEntry Copy() => (TimeEntry)MemberwiseClone();

    [JsonIgnore]
    public bool IsSynced => JiraWorklogId is not null && !SyncPending;

    /// <summary>The issue the entry's worklog is on in Jira: where it was logged, else the entry's own issue.</summary>
    [JsonIgnore]
    public string WorklogIssueKey => JiraWorklogIssueKey ?? IssueKey;

    /// <summary>True when the entry's worklog is on another issue than the entry. Worklogs can't move, so it is logged again.</summary>
    [JsonIgnore]
    public bool MovedToAnotherIssue =>
        JiraWorklogId is not null && !string.Equals(JiraWorklogIssueKey, IssueKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>Jira has the entry now, as worklog <paramref name="worklogId"/> on the entry's issue.</summary>
    public void Sent(string worklogId)
    {
        (_worklogId, _worklogIssueKey) = (worklogId, IssueKey);
        (_syncPending, _syncError) = (false, null);
    }

    /// <summary>Jira didn't take the entry; it waits to be sent again.</summary>
    public void Failed(string reason) => (_syncPending, _syncError) = (true, reason);

    /// <summary>The user removed the work; its worklog is still to be deleted in Jira.</summary>
    public void MarkRemovalPending() => (_deletePending, _syncPending) = (true, true);

    /// <summary>The worklog on the old issue was deleted; the entry is not in Jira until it is logged on its new issue.</summary>
    public void LeftOldIssue() => (_worklogId, _worklogIssueKey) = (null, null);

    /// <summary>Takes the worklog and creation time of <paramref name="read"/>, the same work as read before it was edited.</summary>
    public void TakeWorklogFrom(TimeEntry read)
    {
        (_worklogId, _worklogIssueKey) = (read.JiraWorklogId, read.JiraWorklogIssueKey);
        CreatedUtc = read.CreatedUtc;
    }

    /// <summary>A typed comment as it is kept: trimmed, and none when nothing is left.</summary>
    public static string? CleanComment(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>True when the comments are the same, a missing comment counting as an empty one.</summary>
    public static bool SameComment(string? a, string? b) => (a ?? "") == (b ?? "");

    /// <summary>True when <paramref name="other"/> has the same issue, hours and comment (see <see cref="SameComment"/>).</summary>
    public bool SameWork(TimeEntry other) =>
        IssueKey == other.IssueKey && Duration == other.Duration && SameComment(Comment, other.Comment);

    /// <summary>The hours of the entries added up.</summary>
    public static TimeSpan Total(IEnumerable<TimeEntry> entries) => entries.Select(e => e.Duration).Sum();

    /// <summary>True when any of the entries is not (fully) in Jira.</summary>
    public static bool HasSyncProblem(IEnumerable<TimeEntry> entries) => entries.Any(e => !e.IsSynced);

    /// <summary>Why the entries are not in Jira, each reason once; empty when they all are.</summary>
    public static IReadOnlyList<string> SyncProblems(IEnumerable<TimeEntry> entries) =>
        entries.Where(e => !e.IsSynced).Select(e => e.SyncError ?? NotSentYet).Distinct().ToList();

    /// <summary>The reason shown for an entry not in Jira that has no error (yet).</summary>
    public const string NotSentYet = "Not sent to Jira yet";
}
