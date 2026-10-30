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
}
