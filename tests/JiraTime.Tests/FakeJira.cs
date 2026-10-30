using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiraTime.Tests;

/// <summary>
/// In-memory stand-in for Jira's worklog and issue search endpoints, plugged into HttpClient.
/// Records every request so tests can check what was sent.
/// </summary>
public sealed partial class FakeJira : HttpMessageHandler
{
    public record Request(HttpMethod Method, string Path, JsonNode? Body);

    /// <summary>issue key → worklog id → body.</summary>
    public Dictionary<string, Dictionary<string, JsonNode>> Worklogs { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Request> Requests { get; } = [];

    /// <summary>When set, every request fails with this status.</summary>
    public HttpStatusCode? FailWith { get; set; }

    /// <summary>When set and it returns a status, that request fails with it.</summary>
    public Func<Uri, HttpStatusCode?>? FailWhen { get; set; }

    /// <summary>How many of the next requests Jira refuses with 429 Too Many Requests.</summary>
    public int TooManyRequests { get; set; }

    /// <summary>The Retry-After header sent with a 429 (seconds or an HTTP date); none when null.</summary>
    public string? RetryAfter { get; set; }

    /// <summary>When set, requests wait for it before Jira answers, so tests can see how many are out at once.</summary>
    public TaskCompletionSource? Hold { get; set; }

    /// <summary>The requests <see cref="Hold"/> holds back; all of them when null.</summary>
    public Func<Request, bool>? HoldWhen { get; set; }

    /// <summary>Released once for each request that reached Jira.</summary>
    public SemaphoreSlim Arrived { get; } = new(0);

    /// <summary>The most requests that were out at the same time.</summary>
    public int MostInFlight { get; private set; }

    private readonly Lock _lock = new();
    private int _inFlight;

    /// <summary>When set, requests throw as if the server can't be reached.</summary>
    public bool Offline { get; set; }

    /// <summary>Extra GET handlers: path (without query) → JSON response.</summary>
    public Dictionary<string, Func<Uri, object>> Gets { get; } = new();

    /// <summary>Author put on worklogs that don't name one.</summary>
    public JsonObject DefaultAuthor { get; set; } = new() { ["key"] = "JIRAUSER1", ["name"] = "carl" };

    /// <summary>Adds a worklog as if someone logged it in Jira directly.</summary>
    public string AddWorklog(string issueKey, string started, int seconds, string? comment = null, JsonObject? author = null)
    {
        var id = (_nextId++).ToString();
        var logs = Worklogs.TryGetValue(issueKey, out var l) ? l : Worklogs[issueKey] = [];
        logs[id] = new JsonObject
        {
            ["started"] = started, ["timeSpentSeconds"] = seconds, ["comment"] = comment,
            ["author"] = author ?? DefaultAuthor.DeepClone(),
        };
        return id;
    }

    private int _nextId = 10000;

    public HttpClient Client() => new(this);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (Offline) throw new HttpRequestException("Connection refused");

        var body = req.Content is null ? null : JsonNode.Parse(await req.Content.ReadAsStringAsync(ct));
        var request = new Request(req.Method, req.RequestUri!.PathAndQuery, body);
        lock (_lock)
        {
            Requests.Add(request);
            MostInFlight = Math.Max(MostInFlight, ++_inFlight);
        }
        Arrived.Release();
        try
        {
            if (Hold is { } hold && HoldWhen?.Invoke(request) != false) await hold.Task.WaitAsync(ct);
            lock (_lock) return Answer(req, body);
        }
        finally
        {
            lock (_lock) _inFlight--;
        }
    }

    /// <summary>Jira's answer, one request at a time, so parallel requests see consistent worklogs.</summary>
    private HttpResponseMessage Answer(HttpRequestMessage req, JsonNode? body)
    {
        var path = req.RequestUri!.AbsolutePath;
        if (TooManyRequests > 0)
        {
            TooManyRequests--;
            var refused = Json(HttpStatusCode.TooManyRequests, new { errorMessages = new[] { "Rate limit exceeded" } });
            if (RetryAfter is { } after) refused.Headers.TryAddWithoutValidation("Retry-After", after);
            return refused;
        }

        if ((FailWith ?? FailWhen?.Invoke(req.RequestUri)) is { } status)
            return Json(status, new { errorMessages = new[] { $"Failed with {(int)status}" } });

        if (req.Method == HttpMethod.Get && Gets.TryGetValue(path, out var get))
            return Json(HttpStatusCode.OK, get(req.RequestUri));

        if ((req.Method == HttpMethod.Get || req.Method == HttpMethod.Post) && (path.EndsWith("/search") || path.EndsWith("/search/jql")))
            return Search(FakeSearch.Read(req.RequestUri, body));

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // rest/api/{v}/issue/{key}/worklog[/{id}]
        if (parts.Length >= 6 && parts[3] == "issue" && parts[5] == "worklog")
        {
            var key = parts[4];
            var logs = Worklogs.TryGetValue(key, out var l) ? l : Worklogs[key] = [];
            var id = parts.Length > 6 ? parts[6] : null;

            if (req.Method == HttpMethod.Post)
            {
                var newId = (_nextId++).ToString();
                logs[newId] = body!;
                return Json(HttpStatusCode.Created, new { id = newId });
            }
            if (req.Method == HttpMethod.Get && id is null)
                return Json(HttpStatusCode.OK, new JsonObject
                {
                    ["startAt"] = 0, ["total"] = logs.Count,
                    ["worklogs"] = new JsonArray(logs.Select(w => (JsonNode)WithIdAndAuthor(w.Key, w.Value)).ToArray()),
                });
            if (id is null || !logs.ContainsKey(id))
                return Json(HttpStatusCode.NotFound, new { errorMessages = new[] { "Worklog not found" } });
            if (req.Method == HttpMethod.Get)
                return Json(HttpStatusCode.OK, WithIdAndAuthor(id, logs[id]));
            if (req.Method == HttpMethod.Put)
            {
                logs[id] = body!;
                return Json(HttpStatusCode.OK, new { id });
            }
            if (req.Method == HttpMethod.Delete)
            {
                logs.Remove(id);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
        }
        return Json(HttpStatusCode.NotFound, new { errorMessages = new[] { "No such endpoint" } });
    }

    private JsonObject WithIdAndAuthor(string id, JsonNode body)
    {
        var o = body.DeepClone().AsObject();
        o["id"] = id;
        o["author"] ??= DefaultAuthor.DeepClone();
        return o;
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object body) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
}
