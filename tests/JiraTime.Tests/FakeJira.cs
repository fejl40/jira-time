using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace JiraTime.Tests;

/// <summary>
/// In-memory stand-in for Jira's worklog and issue search endpoints, plugged into HttpClient.
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

    /// <summary>Author put on worklogs that don't name one.</summary>
    public JsonObject DefaultAuthor { get; set; } = new() { ["key"] = "JIRAUSER1", ["name"] = "carl" };

    /// <summary>Issues the worklog search does not return yet (Jira's search index lagging behind).</summary>
    public HashSet<string> NotIndexed { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// issue key → fields, as the issue searches return them (sprint, period, status and epic lookups).
    /// A <c>key in (...)</c> query gets only the keys it names. The worklog search returns the issues
    /// that have worklogs instead, with their fields from here when they have any.
    /// </summary>
    public Dictionary<string, JsonObject> Issues { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Adds an issue with a summary, type and status to <see cref="Issues"/>.</summary>
    public void AddIssue(string key, string summary, string type = "Story", string status = "To Do", string category = "new") =>
        Issues[key] = new JsonObject
        {
            ["summary"] = summary, ["issuetype"] = new JsonObject { ["name"] = type },
            ["status"] = new JsonObject { ["name"] = status, ["statusCategory"] = new JsonObject { ["key"] = category } },
        };

    /// <summary>The JQL of a search request.</summary>
    public static string Jql(Uri uri) => HttpUtility.ParseQueryString(uri.Query)["jql"] ?? "";

    /// <summary>The JQL of a recorded search request.</summary>
    public static string Jql(Request request) => Jql(new Uri("http://jira" + request.Path));

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
        var path = req.RequestUri!.AbsolutePath;
        Requests.Add(new Request(req.Method, path + req.RequestUri.Query, body));

        if ((FailWith ?? FailWhen?.Invoke(req.RequestUri)) is { } status)
            return Json(status, new { errorMessages = new[] { $"Failed with {(int)status}" } });

        if (req.Method == HttpMethod.Get && Gets.TryGetValue(path, out var get))
            return Json(HttpStatusCode.OK, get(req.RequestUri));

        if (req.Method == HttpMethod.Get && (path.EndsWith("/search") || path.EndsWith("/search/jql")))
            return Json(HttpStatusCode.OK, Search(Jql(req.RequestUri)));

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

    private JsonObject Search(string jql)
    {
        var keys = jql.Contains("worklogAuthor")
            ? Worklogs.Where(w => w.Value.Count > 0 && !NotIndexed.Contains(w.Key)).Select(w => w.Key).ToList()
            : Issues.Keys.Where(k => !jql.StartsWith("key in") || jql.Contains($"\"{k}\"", StringComparison.OrdinalIgnoreCase)).ToList();
        var issues = keys.Select(k => (JsonNode)new JsonObject
        {
            ["key"] = k,
            ["fields"] = Issues.TryGetValue(k, out var fields) ? fields.DeepClone() : new JsonObject { ["summary"] = $"Summary of {k}" },
        });
        return new JsonObject { ["startAt"] = 0, ["total"] = keys.Count, ["issues"] = new JsonArray(issues.ToArray()) };
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
