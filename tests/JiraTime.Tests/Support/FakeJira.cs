using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiraTime.Tests.Support;

/// <summary>
/// In-memory stand-in for Jira's myself, worklog and issue search endpoints, plugged into HttpClient.
/// Records every request so tests can check what was sent.
/// </summary>
public sealed partial class FakeJira : HttpMessageHandler
{
    /// <param name="Host">The host it was sent to.</param>
    /// <param name="Authorized">Whether it carried the token.</param>
    public record Request(HttpMethod Method, string Path, JsonNode? Body, string Host = "", bool Authorized = false);

    /// <summary>issue key → worklog id → body.</summary>
    public Dictionary<string, Dictionary<string, JsonNode>> Worklogs { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Request> Requests { get; } = [];

    /// <summary>Worklogs created, changed or deleted.</summary>
    public IEnumerable<Request> Writes => Requests.Where(r => IsWorklog(r) && r.Method != HttpMethod.Get);

    /// <summary>Worklogs read on their own, one issue (or one worklog) at a time.</summary>
    public IEnumerable<Request> WorklogReads => Requests.Where(r => IsWorklog(r) && r.Method == HttpMethod.Get);

    private static bool IsWorklog(Request request) => PathOf(request).Contains("/worklog");

    /// <summary>The request's path without its query.</summary>
    private static string PathOf(Request request) => request.Path.Split('?')[0];

    private readonly Lock _lock = new();

    /// <summary>Extra GET handlers: path (without query) → JSON response.</summary>
    public Dictionary<string, Func<Uri, object>> Gets { get; } = new();

    /// <summary>Pictures (such as avatars) by path without query: bytes and media type, on any host.</summary>
    public Dictionary<string, (byte[] Bytes, string ContentType)> Pictures { get; } = new();

    /// <summary>Who Jira says the token belongs to (<c>/myself</c>): Carl unless a test needs someone else.</summary>
    public object Myself { get; set; } = new { key = "JIRAUSER1", name = "carl", displayName = "Carl Lilholm" };

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

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (Offline) throw new HttpRequestException("Connection refused");

        var body = req.Content is null ? null : JsonNode.Parse(await req.Content.ReadAsStringAsync(ct));
        var request = new Request(req.Method, req.RequestUri!.PathAndQuery, body, req.RequestUri.Host, req.Headers.Authorization is not null);
        lock (_lock)
        {
            Requests.Add(request);
            Arrive();
        }
        Arrived.Release();
        try
        {
            await HoldAsync(request, ct);
            lock (_lock) return Refusal(req.RequestUri) ?? Answer(req, body);
        }
        finally
        {
            lock (_lock) Leave();
        }
    }

    /// <summary>Jira's answer, one request at a time, so parallel requests see consistent worklogs.</summary>
    private HttpResponseMessage Answer(HttpRequestMessage req, JsonNode? body)
    {
        var path = req.RequestUri!.AbsolutePath;
        if (req.Method == HttpMethod.Get && Gets.TryGetValue(path, out var get))
            return Json(HttpStatusCode.OK, get(req.RequestUri));

        if (req.Method == HttpMethod.Get && Pictures.TryGetValue(path, out var picture))
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(picture.Bytes) { Headers = { ContentType = new(picture.ContentType) } } };

        if (req.Method == HttpMethod.Get && path.EndsWith("/myself"))
            return Json(HttpStatusCode.OK, Myself);

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
