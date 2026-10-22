using System.Text.Json.Serialization;
using JiraTime.Storage;

namespace JiraTime.Domain;

public enum JiraAuthType
{
    /// <summary>Server / Data Center Personal Access Token, sent as Bearer.</summary>
    Pat,
    /// <summary>Jira Cloud: email + API token, sent as Basic.</summary>
    Cloud,
}

/// <summary>
/// The one server-side settings document (id "app"). The Jira connection and token are
/// NOT here: they live encrypted in the browser, see <c>Services.ConnectionState</c>.
/// </summary>
[DocumentCollection("settings")]
/// <summary>
/// The single settings document from before multi-user support. Only read to find out whose the old
/// data is; the profile now comes from Jira on each visit.
/// </summary>
public class AppSettings : IDocument
{
    public const string SingletonId = "app";
    public string Id { get; set; } = SingletonId;

    public JiraProfile? Profile { get; set; }
}

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

    /// <summary>Time of day put on the Jira worklog, which needs one.</summary>
    public static readonly TimeOnly JiraStartTime = new(9, 0);

    /// <summary>Most hours one entry can have.</summary>
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);
    public string? Description { get; set; }

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

public record JiraConnection(string BaseUrl, JiraAuthType AuthType, string Token, string? Email)
{
    [JsonIgnore]
    public bool IsCloud => AuthType == JiraAuthType.Cloud;
    [JsonIgnore]
    public string Host => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var u) ? u.Host : BaseUrl;
}

/// <param name="Id">accountId on Cloud, user key on Server/DC.</param>
/// <param name="Username">Login name on Server/DC; null on Cloud.</param>
public record JiraProfile(string Id, string? Username, string DisplayName, string? Email)
{
    [JsonIgnore]
    public string Initials => string.Concat(
        DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));

    [JsonIgnore]
    public string Subtitle => string.Join(" · ", new[] { Username, Email }.Where(s => !string.IsNullOrEmpty(s)));
}

public record JiraIssue(string Key, string Summary);

/// <summary>A worklog as Jira has it, read back for import.</summary>
public record JiraWorklog(string Id, string IssueKey, string IssueSummary, DateOnly Date, TimeSpan Duration, string? Comment);

/// <summary>An issue assigned to the user in an open sprint.</summary>
/// <param name="StatusCategory">Jira's status category key: "new", "indeterminate" (in progress) or "done".</param>
public record SprintIssue(string Key, string Summary, string Type, string Status, string StatusCategory)
{
    [JsonIgnore]
    public bool IsInProgress => StatusCategory == "indeterminate";

    public JiraIssue ToIssue() => new(Key, Summary);
}

/// <summary>
/// A semi-monthly timesheet period: the 1st to the 15th, or the 16th to the last day of the month.
/// </summary>
public readonly record struct Period
{
    public const int SplitDay = 15;

    public DateOnly Start { get; }
    public DateOnly End { get; }

    private Period(DateOnly start, DateOnly end) => (Start, End) = (start, end);

    public int Length => End.DayNumber - Start.DayNumber + 1;
    public Period Next => For(End.AddDays(1));
    public Period Previous => For(Start.AddDays(-1));
    public IEnumerable<DateOnly> Days => Enumerable.Range(0, Length).Select(Start.AddDays);
    public bool Contains(DateOnly d) => d >= Start && d <= End;

    /// <summary>The period containing <paramref name="date"/>.</summary>
    public static Period For(DateOnly date)
    {
        if (date.Day <= SplitDay)
            return new Period(new DateOnly(date.Year, date.Month, 1), new DateOnly(date.Year, date.Month, SplitDay));

        var last = DateTime.DaysInMonth(date.Year, date.Month);
        return new Period(new DateOnly(date.Year, date.Month, SplitDay + 1), new DateOnly(date.Year, date.Month, last));
    }
}

/// <summary>Copy of the data from before multi-user support, stored before it was given to its owner.</summary>
[DocumentCollection("backups")]
public class LegacyBackup : IDocument
{
    public string Id { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public UserKey? ClaimedBy { get; set; }
    public List<TimeEntry> Entries { get; set; } = [];
    public List<DayOff> DaysOff { get; set; } = [];
    public AppSettings? Settings { get; set; }
}
