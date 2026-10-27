using System.Text.Json.Serialization;
using JiraTime.Storage;

namespace JiraTime.Domain;

/// <summary>One block of work on one Jira issue, on one day.</summary>
[DocumentCollection("time-entries")]
public class TimeEntry : IDocument
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

    /// <summary>The worklog comment.</summary>
    public string? Description { get; set; }

    /// <summary>Time of day put on the Jira worklog, which needs one.</summary>
    public static readonly TimeOnly JiraStartTime = new(9, 0);

    /// <summary>Most hours one entry, or one day, can have.</summary>
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);

    /// <summary>Set once the entry has been pushed to Jira as a worklog.</summary>
    public string? JiraWorklogId { get; set; }

    /// <summary>The issue the worklog lives on. Differs from <see cref="IssueKey"/> after the entry moved to another issue.</summary>
    public string? JiraWorklogIssueKey { get; set; }

    /// <summary>The worklog was logged in Jira (not in this app) and imported.</summary>
    public bool FromJira { get; set; }

    /// <summary>True when the entry changed locally and Jira has not caught up yet.</summary>
    public bool SyncPending { get; set; } = true;

    /// <summary>
    /// The user removed this work, but Jira still has its worklog. Hidden everywhere except
    /// "not in Jira"; the worklog delete is retried until it succeeds, then the entry is dropped.
    /// </summary>
    public bool DeletePending { get; set; }

    /// <summary>Why the last attempt to sync failed, if it did.</summary>
    public string? SyncError { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    // Entries saved before TASK-12 had start and end times instead of a duration. They are read as
    // their duration and written back without the times on the next save.
    private TimeOnly? _legacyStart, _legacyEnd;

    [JsonInclude, JsonPropertyName("start"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeOnly? LegacyStart { get => null; set { _legacyStart = value; FromLegacy(); } }

    [JsonInclude, JsonPropertyName("end"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeOnly? LegacyEnd { get => null; set { _legacyEnd = value; FromLegacy(); } }

    /// <summary>A separate copy to edit without touching this one.</summary>
    public TimeEntry Copy() => (TimeEntry)MemberwiseClone();

    private void FromLegacy()
    {
        if (Duration == TimeSpan.Zero && _legacyStart is { } a && _legacyEnd is { } b && b > a) Duration = b - a;
    }

    [JsonIgnore]
    public bool IsSynced => JiraWorklogId is not null && !SyncPending;
}
