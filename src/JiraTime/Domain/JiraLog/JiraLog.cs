using System.Text.Json.Serialization;

namespace JiraTime.Domain;

/// <summary>
/// One Jira request as the request log keeps it: what was sent, what came back, how long it took and the
/// user action it was made for. While the request is out, <see cref="DurationMs"/> is null.
/// </summary>
public sealed record JiraLogEntry
{
    /// <summary>The action name for requests made outside any user action.</summary>
    public const string Background = "Background";

    public string Id { get; init; } = "";
    /// <summary>When the request went out, in the server's time zone.</summary>
    public DateTimeOffset Time { get; init; }
    public string Owner { get; init; } = "";
    public string Action { get; init; } = Background;
    /// <summary>Shared by every request of one user action; null for background requests.</summary>
    public string? ActionId { get; init; }
    public string Method { get; init; } = "";
    public string Url { get; init; } = "";
    public int? Status { get; init; }
    public string? Reason { get; init; }
    public long? DurationMs { get; init; }
    public Dictionary<string, string> RequestHeaders { get; init; } = [];
    public Dictionary<string, string> ResponseHeaders { get; init; } = [];
    public string? ContentType { get; init; }
    /// <summary>Bytes in the response body.</summary>
    public long? ResponseSize { get; init; }
    public string? RequestBody { get; init; }
    public bool RequestBodyTruncated { get; init; }
    public string? ResponseBody { get; init; }
    public bool ResponseBodyTruncated { get; init; }
    /// <summary>Why the request failed when Jira gave no usable answer: unreachable, timed out, rejected.</summary>
    public string? Error { get; init; }

    /// <summary>Path and query of <see cref="Url"/>; written to the file too, so it is easy to search.</summary>
    public string Path => Uri.TryCreate(Url, UriKind.Absolute, out var uri) ? uri.PathAndQuery : Url;

    [JsonIgnore] public bool IsPending => DurationMs is null;

    [JsonIgnore] public bool Failed => Error is not null || Status >= 400;

    /// <summary>"pending", "ok" or "error"; the toasts and the Logs page mark requests with it.</summary>
    [JsonIgnore] public string State => IsPending ? "pending" : Failed ? "error" : "ok";

    /// <summary>The path without the REST prefix and query: "issue/ABC-1/worklog".</summary>
    [JsonIgnore]
    public string ShortPath
    {
        get
        {
            var path = Path.Split('?')[0];
            foreach (var prefix in new[] { "/rest/api/3/", "/rest/api/2/" })
                if (path.Contains(prefix)) return path[(path.IndexOf(prefix) + prefix.Length)..];
            return path;
        }
    }

    /// <summary>
    /// How the request ended, in a few words: "200 · 1.2 s", "Waiting for Jira…". <paramref name="duration"/> is
    /// <see cref="DurationMs"/> as the UI writes it, so the rule stays here and the formatting in the UI.
    /// </summary>
    public string Outcome(string duration) => this switch
    {
        { IsPending: true } => "Waiting for Jira…",
        { Status: { } status, Error: null } => $"{status} · {duration}",
        { Status: { } status } => $"{status} {Error} · {duration}",
        _ => $"{Error} · {duration}",
    };
}

/// <summary>The Logs page's filters: only failed requests, and text to find in a request.</summary>
public sealed record JiraLogFilter(bool ErrorsOnly = false, string Text = "")
{
    public bool Matches(JiraLogEntry e)
    {
        if (ErrorsOnly && !e.Failed) return false;
        var text = Text.Trim();
        if (text == "") return true;
        string?[] fields = [e.Action, e.Method, e.Url, e.Status?.ToString(), e.Reason, e.Error, e.RequestBody, e.ResponseBody];
        return fields.Any(f => f?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
    }
}

/// <summary>The requests of one user action (or a run of background requests), newest first.</summary>
public sealed record JiraLogGroup(string Key, string Action, IReadOnlyList<JiraLogEntry> Requests)
{
    public DateTimeOffset Time => Requests[0].Time;
    public int Failed => Requests.Count(r => r.Failed);
}

/// <summary>
/// One day of the user's Jira log as the Logs page shows it: read from the file, then kept up to date with
/// live requests, and grouped by the user action that made them.
/// </summary>
public sealed class JiraLogDay
{
    private readonly Dictionary<string, JiraLogEntry> _byId = [];

    public JiraLogDay(DateOnly day, IEnumerable<JiraLogEntry> entries)
    {
        Day = day;
        foreach (var entry in entries) Update(entry);
    }

    public DateOnly Day { get; }
    public int Count => _byId.Count;

    /// <summary>Adds a request or brings it up to date. A start that arrives after its finish is ignored.</summary>
    public void Update(JiraLogEntry entry)
    {
        if (entry.IsPending && _byId.TryGetValue(entry.Id, out var known) && !known.IsPending) return;
        _byId[entry.Id] = entry;
    }

    /// <summary>
    /// The requests that pass <paramref name="filter"/>, newest first, grouped by user action. Background
    /// requests next to each other share a group.
    /// </summary>
    public IReadOnlyList<JiraLogGroup> Groups(JiraLogFilter filter)
    {
        var newestFirst = _byId.Values.Where(filter.Matches).OrderByDescending(e => e.Time).ToList();
        var groups = new List<(string Key, string Action, List<JiraLogEntry> Requests)>();
        var byAction = new Dictionary<string, List<JiraLogEntry>>();
        foreach (var entry in newestFirst)
        {
            if (entry.ActionId is { } id && byAction.TryGetValue(id, out var requests))
            {
                requests.Add(entry);
                continue;
            }
            if (entry.ActionId is null && groups.Count > 0 && groups[^1].Key.StartsWith("background:"))
            {
                groups[^1].Requests.Add(entry);
                continue;
            }
            var group = (entry.ActionId ?? "background:" + entry.Id, entry.Action, new List<JiraLogEntry> { entry });
            groups.Add(group);
            if (entry.ActionId is { } newId) byAction[newId] = group.Item3;
        }
        return groups.Select(g => new JiraLogGroup(g.Key, g.Action, g.Requests)).ToList();
    }
}
