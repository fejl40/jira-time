using System.Text.Json.Serialization;

namespace JiraTime.Domain;

public enum JiraAuthType
{
    /// <summary>Server / Data Center Personal Access Token, sent as Bearer.</summary>
    Pat,
    /// <summary>Jira Cloud: email + API token, sent as Basic.</summary>
    Cloud,
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
