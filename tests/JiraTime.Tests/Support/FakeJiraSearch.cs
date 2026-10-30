using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;

namespace JiraTime.Tests;

/// <summary>The issue searches of <see cref="FakeJira"/>: worklog, sprint, period, status and key-in searches, GET or POST.</summary>
public sealed partial class FakeJira
{
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

    /// <summary>Issue searches, GET or POST.</summary>
    public IEnumerable<Request> Searches => Requests.Where(r => PathOf(r).EndsWith("/search") || PathOf(r).EndsWith("/search/jql"));

    /// <summary>Searches for the user's worklogs (<c>worklogAuthor</c>), not for issues by key, sprint or period.</summary>
    public IEnumerable<Request> WorklogSearches => Searches.Where(r => Jql(r).Contains("worklogAuthor"));

    /// <summary>The JQL of a search request.</summary>
    public static string Jql(Uri uri) => HttpUtility.ParseQueryString(uri.Query)["jql"] ?? "";

    /// <summary>The JQL of a recorded search request, from its URL or, for a POST search, its body.</summary>
    public static string Jql(Request request) =>
        request.Body?["jql"]?.GetValue<string>() ?? Jql(new Uri("http://jira" + request.Path));

    /// <summary>How many worklogs a search sends along with each issue when asked for the worklog field.</summary>
    public int EmbeddedWorklogLimit { get; set; } = 20;

    /// <summary>Most issues one search page returns, whatever maxResults asks for.</summary>
    public int SearchPageLimit { get; set; } = 100;

    /// <summary>
    /// Answers a search like Jira: a key Jira doesn't know (in neither <see cref="Issues"/> nor <see cref="Worklogs"/>)
    /// fails a <c>key in (...)</c> search with 400 unless it asks validateQuery=warn, which skips it with a warning.
    /// </summary>
    private HttpResponseMessage Search(FakeSearch q)
    {
        var named = q.NamedKeys();
        var unknown = named?.Where(k => !Issues.ContainsKey(k) && !Worklogs.ContainsKey(k)).ToList() ?? [];
        if (unknown.Count > 0 && q.ValidateQuery is not ("warn" or "none"))
            return Json(HttpStatusCode.BadRequest, new { errorMessages = unknown.Select(k => $"An issue with key '{k}' does not exist for field 'key'.").ToArray() });

        var keys = q.Jql.Contains("worklogAuthor")
            ? Worklogs.Where(w => w.Value.Count > 0 && !NotIndexed.Contains(w.Key)).Select(w => w.Key).ToList()
            : named is not null
                ? named.Except(unknown, StringComparer.OrdinalIgnoreCase).ToList()
                : Issues.Keys.ToList();
        var page = keys.Skip(q.StartAt).Take(Math.Min(q.MaxResults, SearchPageLimit)).ToList();
        var issues = page.Select(k =>
        {
            var fields = Issues.TryGetValue(k, out var f) ? f.DeepClone().AsObject() : new JsonObject { ["summary"] = $"Summary of {k}" };
            if (q.Fields.Contains("worklog")) fields["worklog"] = EmbeddedWorklogs(k);
            return (JsonNode)new JsonObject { ["key"] = k, ["fields"] = fields };
        });
        var answer = new JsonObject { ["startAt"] = q.StartAt, ["total"] = keys.Count, ["issues"] = new JsonArray(issues.ToArray()) };
        var next = q.StartAt + page.Count;
        if (next < keys.Count) answer["nextPageToken"] = next.ToString();
        if (unknown.Count > 0) answer["warningMessages"] = new JsonArray(unknown.Select(k => (JsonNode)$"The issue key '{k}' for field 'key' is invalid.").ToArray());
        return Json(HttpStatusCode.OK, answer);
    }

    /// <summary>The first worklogs of the issue, as Jira puts them in a search answer, with how many there are.</summary>
    private JsonObject EmbeddedWorklogs(string key)
    {
        var logs = Worklogs.TryGetValue(key, out var l) ? l : [];
        var first = logs.Take(EmbeddedWorklogLimit).Select(w => (JsonNode)WithIdAndAuthor(w.Key, w.Value));
        return new JsonObject
        {
            ["startAt"] = 0, ["maxResults"] = EmbeddedWorklogLimit, ["total"] = logs.Count, ["worklogs"] = new JsonArray(first.ToArray()),
        };
    }
}

/// <summary>What a search asks for, read from the URL of a GET search or the JSON body of a POST one.</summary>
internal sealed record FakeSearch(string Jql, string Fields, int StartAt, int MaxResults, string? ValidateQuery)
{
    public static FakeSearch Read(Uri uri, JsonNode? body)
    {
        if (body is not null)
        {
            var fields = body["fields"] is JsonArray list ? string.Join(",", list.Select(f => f!.GetValue<string>())) : "";
            var start = body["startAt"]?.GetValue<int>() ?? int.Parse(body["nextPageToken"]?.GetValue<string>() ?? "0");
            return new(body["jql"]?.GetValue<string>() ?? "", fields, start, body["maxResults"]?.GetValue<int>() ?? 50,
                body["validateQuery"]?.GetValue<string>());
        }
        var q = HttpUtility.ParseQueryString(uri.Query);
        return new(q["jql"] ?? "", q["fields"] ?? "", int.Parse(q["startAt"] ?? q["nextPageToken"] ?? "0"),
            int.Parse(q["maxResults"] ?? "50"), q["validateQuery"]);
    }

    /// <summary>The keys a <c>key in (...)</c> search names; null for any other search.</summary>
    public List<string>? NamedKeys() =>
        Jql.StartsWith("key in")
            ? Regex.Matches(Jql, "\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList()
            : null;
}

