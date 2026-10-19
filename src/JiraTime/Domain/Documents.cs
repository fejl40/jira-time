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

    /// <summary>Any day that starts a two-week period. Periods repeat every 14 days from here.</summary>
    public DateOnly PeriodAnchor { get; set; } = new(2026, 1, 5);
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

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public TimeSpan Duration => End - Start;
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

/// <summary>A two-week timesheet period.</summary>
public readonly record struct Period(DateOnly Start)
{
    public const int Length = 14;
    public DateOnly End => Start.AddDays(Length - 1);
    public Period Next => new(Start.AddDays(Length));
    public Period Previous => new(Start.AddDays(-Length));
    public IEnumerable<DateOnly> Days => Enumerable.Range(0, Length).Select(Start.AddDays);
    public bool Contains(DateOnly d) => d >= Start && d <= End;

    /// <summary>The period containing <paramref name="date"/>, with periods repeating every 14 days from <paramref name="anchor"/>.</summary>
    public static Period For(DateOnly date, DateOnly anchor)
    {
        var offset = (int)Math.Floor((date.DayNumber - anchor.DayNumber) / (double)Length);
        return new Period(anchor.AddDays(offset * Length));
    }
}
