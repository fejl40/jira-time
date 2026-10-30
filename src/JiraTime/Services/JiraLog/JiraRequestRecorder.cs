using System.Net.Http.Headers;
using System.Text;
using JiraTime.Domain;
using JiraTime.Domain.JiraLog;
using JiraTime.Services.Jira;

namespace JiraTime.Services.JiraLog;

/// <summary>
/// Records every Jira request for the request log and the live feed. <see cref="JiraHttp"/> starts a
/// <see cref="JiraCall"/> for each request and finishes it when the request ends, however it ends.
/// Logging never breaks a request: a log file that can't be written is reported and skipped.
/// </summary>
public sealed class JiraRequestRecorder(JiraRequestLog log, JiraRequestFeed feed, TimeProvider clock, ILogger<JiraRequestRecorder> logger)
{
    /// <summary>Bodies longer than this many characters are cut and marked truncated.</summary>
    public const int MaxBody = 64 * 1024;

    /// <summary>Headers that carry credentials; they are never logged.</summary>
    private static readonly HashSet<string> Secret = new(StringComparer.OrdinalIgnoreCase)
        { "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie" };

    /// <summary>Records <paramref name="request"/> going out; <paramref name="body"/> is the JSON sent, if any.</summary>
    public JiraCall Start(JiraConnection c, HttpRequestMessage request, string? body)
    {
        var (requestBody, truncated) = Cap(body);
        var action = JiraAction.Current;
        var entry = new JiraLogEntry
        {
            Id = Guid.NewGuid().ToString("N")[..16],
            Time = clock.GetLocalNow(),
            Owner = c.Owner ?? action?.Owner ?? "",
            Action = action?.Name ?? JiraLogEntry.Background,
            ActionId = action?.Id,
            Method = request.Method.Method,
            Url = request.RequestUri?.AbsoluteUri ?? "",
            RequestHeaders = Headers(request.Headers, request.Content?.Headers),
            RequestBody = requestBody,
            RequestBodyTruncated = truncated,
        };
        var call = new JiraCall(this, entry, action, clock.GetTimestamp());
        if (entry.Owner != "") feed.Publish(entry.Owner, new JiraRequestEvent(JiraRequestStage.Started, entry));
        return call;
    }

    /// <summary>
    /// Logs the finished request. One made while signing in, before anyone knew whose token it was, waits
    /// for its action to learn the owner, and goes to the signed-out folder when it never does.
    /// </summary>
    internal async Task FinishAsync(JiraLogEntry entry, JiraAction? action)
    {
        if (entry.Owner == "" && action?.Owner is { } owner) entry = entry with { Owner = owner };
        if (entry.Owner != "")
        {
            feed.Publish(entry.Owner, new JiraRequestEvent(JiraRequestStage.Finished, entry));
            await WriteAsync(entry);
        }
        else if (action is not null)
            action.WhenOwnerKnown(known => _ = WriteAsync(entry with { Owner = known ?? JiraRequestLog.SignedOut }));
        else
            await WriteAsync(entry with { Owner = JiraRequestLog.SignedOut });
    }

    private async Task WriteAsync(JiraLogEntry entry)
    {
        try
        {
            await log.AppendAsync(entry);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not write the Jira request log.");
        }
    }

    internal TimeSpan Elapsed(long since) => clock.GetElapsedTime(since);

    internal static (string? Text, bool Truncated) Cap(string? text) =>
        text is { Length: > MaxBody } ? (text[..MaxBody], true) : (string.IsNullOrEmpty(text) ? null : text, false);

    /// <summary>Header names and values, without the ones that carry credentials.</summary>
    internal static Dictionary<string, string> Headers(HttpHeaders headers, HttpHeaders? contentHeaders)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, values) in contentHeaders is null ? headers : headers.Concat(contentHeaders))
            if (!Secret.Contains(name)) result[name] = string.Join(", ", values);
        return result;
    }
}

/// <summary>One Jira request being recorded: what came back is added as it arrives, and it is logged when finished.</summary>
public sealed class JiraCall
{
    private readonly JiraRequestRecorder _recorder;
    private readonly JiraAction? _action;
    private readonly long _started;
    private JiraLogEntry _entry;
    private bool _finished;

    internal JiraCall(JiraRequestRecorder recorder, JiraLogEntry entry, JiraAction? action, long started) =>
        (_recorder, _entry, _action, _started) = (recorder, entry, action, started);

    /// <summary>Reads and records Jira's answer. The content stays buffered, so it can be read again.</summary>
    public async Task ReadResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        // A picture (the profile picture) is logged by its type and size; its bytes aren't text.
        var isImage = response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
        var (body, truncated) = isImage ? (null, false) : JiraRequestRecorder.Cap(EncodingOf(response).GetString(bytes));
        _entry = _entry with
        {
            Status = (int)response.StatusCode,
            Reason = response.ReasonPhrase,
            ResponseHeaders = JiraRequestRecorder.Headers(response.Headers, response.Content.Headers),
            ContentType = response.Content.Headers.ContentType?.ToString(),
            ResponseSize = bytes.Length,
            ResponseBody = body,
            ResponseBodyTruncated = truncated,
        };
    }

    private static Encoding EncodingOf(HttpResponseMessage response)
    {
        try
        {
            return response.Content.Headers.ContentType?.CharSet is { } charset ? Encoding.GetEncoding(charset.Trim('"')) : Encoding.UTF8;
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8; // a charset .NET doesn't know
        }
    }

    /// <summary>Logs the request with how long it took; <paramref name="error"/> says why it failed, if it did.</summary>
    public async Task FinishAsync(string? error)
    {
        if (_finished) return;
        _finished = true;
        _entry = _entry with { Error = error, DurationMs = (long)_recorder.Elapsed(_started).TotalMilliseconds };
        await _recorder.FinishAsync(_entry, _action);
    }
}
