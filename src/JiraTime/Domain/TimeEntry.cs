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

    private void FromLegacy()
    {
        if (Duration == TimeSpan.Zero && _legacyStart is { } a && _legacyEnd is { } b && b > a) Duration = b - a;
    }

    [JsonIgnore]
    public bool IsSynced => JiraWorklogId is not null && !SyncPending;

    /// <summary>The issue the entry's worklog is on in Jira: where it was logged, else the entry's own issue.</summary>
    [JsonIgnore]
    public string WorklogIssueKey => JiraWorklogIssueKey ?? IssueKey;

    /// <summary>True when the entry's worklog is on another issue than the entry. Worklogs can't move, so it is logged again.</summary>
    [JsonIgnore]
    public bool MovedToAnotherIssue =>
        JiraWorklogId is not null && !string.Equals(JiraWorklogIssueKey, IssueKey, StringComparison.OrdinalIgnoreCase);
}
