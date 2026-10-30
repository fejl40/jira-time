using System.Text.Json.Serialization;

namespace JiraTime.Domain;

public enum JiraAuthType
{
    /// <summary>Server / Data Center Personal Access Token, sent as Bearer.</summary>
    Pat,
    /// <summary>Jira Cloud: email + API token, sent as Basic.</summary>
    Cloud,
}

/// <summary>How to reach Jira: its URL, the kind of token and the token itself.</summary>
public record JiraConnection(string BaseUrl, JiraAuthType AuthType, string Token, string? Email)
{
    [JsonIgnore]
    public bool IsCloud => AuthType == JiraAuthType.Cloud;
    [JsonIgnore]
    public string Host => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var u) ? u.Host : BaseUrl;

    /// <summary>
    /// Owner id (<see cref="UserKey.Id"/>) of the user Jira confirmed this token for; null until then.
    /// Requests are logged under it.
    /// </summary>
    [JsonIgnore]
    public string? Owner { get; init; }

    /// <summary>
    /// The time zone of the user's browser, read at sign-in: today and the worklogs' start time are on its clock.
    /// Null means the server's zone (the browser gave none, or one the server doesn't know).
    /// </summary>
    [JsonIgnore]
    public TimeZoneInfo? TimeZone { get; init; }

    /// <summary>
    /// The Jira URL as typed, trimmed, without a trailing slash and with https:// when no scheme is given; null when
    /// it isn't an http or https URL with a host (such as https:// alone).
    /// </summary>
    public static string? NormalizeUrl(string typed)
    {
        var url = typed.Trim();
        if (!url.Contains("://", StringComparison.Ordinal)) url = "https://" + url;
        url = url.TrimEnd('/');
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && uri.Host != "" ? url : null;
    }
}

/// <param name="Id">accountId on Cloud, user key on Server/DC.</param>
/// <param name="Username">Login name on Server/DC; null on Cloud.</param>
/// <param name="AvatarUrl">The 48 px profile picture Jira gave in /myself; null when it gave none.</param>
public record JiraProfile(string Id, string? Username, string DisplayName, string? Email, string? AvatarUrl = null)
{
    [JsonIgnore]
    public string Initials => string.Concat(
        DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));

    [JsonIgnore]
    public string Subtitle => string.Join(" · ", new[] { Username, Email }.Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>A Jira issue: its key and summary.</summary>
public record JiraIssue(string Key, string Summary)
{
    /// <summary>The issue's page in Jira.</summary>
    public static string Url(string baseUrl, string key) => $"{baseUrl.TrimEnd('/')}/browse/{key}";
}

/// <summary>A worklog as Jira has it.</summary>
/// <param name="Created">When it was logged; orders a day's work. Null when Jira didn't say.</param>
public record JiraWorklog(string Id, string IssueKey, string IssueSummary, DateOnly Date, TimeSpan Duration, string? Comment, DateTimeOffset? Created = null);

/// <summary>An issue with its current status (from a sprint, period or status lookup).</summary>
/// <param name="StatusCategory">Jira's status category key: "new", "indeterminate" (in progress) or "done".</param>
public record IssueWithStatus(string Key, string Summary, string Type, string Status, string StatusCategory)
{
    [JsonIgnore]
    public bool IsInProgress => StatusCategory == "indeterminate";

    /// <summary>Finished in Jira (status category Done): no longer to do.</summary>
    [JsonIgnore]
    public bool IsDone => StatusCategory == "done";

    public JiraIssue ToIssue() => new(Key, Summary);
}
