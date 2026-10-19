using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiraTime.Tests;

/// <summary>
/// In-memory stand-in for Jira's worklog endpoints, plugged into HttpClient.
/// Records every request so tests can check what was sent.
/// </summary>
public sealed class FakeJira : HttpMessageHandler
{
    public record Request(HttpMethod Method, string Path, JsonNode? Body);

    /// <summary>issue key → worklog id → body.</summary>
    public Dictionary<string, Dictionary<string, JsonNode>> Worklogs { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Request> Requests { get; } = [];

    /// <summary>When set, every request fails with this status.</summary>
    public HttpStatusCode? FailWith { get; set; }

    /// <summary>When set and it returns a status, that request fails with it.</summary>
    public Func<Uri, HttpStatusCode?>? FailWhen { get; set; }

    /// <summary>When set, requests throw as if the server can't be reached.</summary>
    public bool Offline { get; set; }

    /// <summary>Extra GET handlers: path (without query) → JSON response.</summary>
    public Dictionary<string, Func<Uri, object>> Gets { get; } = new();

    private int _nextId = 10000;

    public HttpClient Client() => new(this);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (Offline) throw new HttpRequestException("Connection refused");

        var body = req.Content is null ? null : JsonNode.Parse(await req.Content.ReadAsStringAsync(ct));
        var path = req.RequestUri!.AbsolutePath;
        Requests.Add(new Request(req.Method, path + req.RequestUri.Query, body));

        if ((FailWith ?? FailWhen?.Invoke(req.RequestUri)) is { } status)
            return Json(status, new { errorMessages = new[] { $"Failed with {(int)status}" } });

        if (req.Method == HttpMethod.Get && Gets.TryGetValue(path, out var get))
            return Json(HttpStatusCode.OK, get(req.RequestUri));

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
            if (id is null || !logs.ContainsKey(id))
                return Json(HttpStatusCode.NotFound, new { errorMessages = new[] { "Worklog not found" } });
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

    private static HttpResponseMessage Json(HttpStatusCode status, object body) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
}
