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
public class AppSettings : IDocument
{
    public const string SingletonId = "app";
    public string Id { get; set; } = SingletonId;

    public JiraProfile? Profile { get; set; }

    /// <summary>When a normal workday runs; used to find gaps and place suggested entries.</summary>
    public Workday Workday { get; set; } = Workday.Default;
}

/// <summary>One block of work on one Jira issue, on one day.</summary>
[DocumentCollection("time-entries")]
public class TimeEntry : IDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public required string IssueKey { get; set; }
    /// <summary>Cached so the timesheet renders without calling Jira.</summary>
    public string IssueSummary { get; set; } = "";

    public DateOnly Date { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string? Description { get; set; }

    /// <summary>Set once the entry has been pushed to Jira as a worklog.</summary>
    public string? JiraWorklogId { get; set; }

    /// <summary>The issue the worklog lives on. Differs from <see cref="IssueKey"/> after the entry moved to another issue.</summary>
    public string? JiraWorklogIssueKey { get; set; }

    /// <summary>True when the entry changed locally and Jira has not caught up yet.</summary>
    public bool SyncPending { get; set; } = true;

    /// <summary>Why the last attempt to sync failed, if it did.</summary>
    public string? SyncError { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public TimeSpan Duration => End - Start;

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
