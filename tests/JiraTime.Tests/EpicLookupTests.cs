using System.Net;
using System.Text.Json.Nodes;
using System.Web;
using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

public class EpicLookupTests
{
    private static readonly JiraConnection Server = TimeHarness.Server;

    private readonly FakeJira _jira = new();
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));
    private readonly JiraIssueLookup _issues;

    /// <summary>What the fake Jira's search returns per key: the issue's fields.</summary>
    private readonly Dictionary<string, JsonObject> _fields = new(StringComparer.OrdinalIgnoreCase);

    private bool _hasEpicLink = true;

    public EpicLookupTests()
    {
        _jira.Gets["/rest/api/2/field"] = _ => _hasEpicLink
            ? new JsonArray(
                new JsonObject { ["id"] = "summary", ["name"] = "Summary" },
                new JsonObject { ["id"] = "customfield_10008", ["name"] = "Epic Link", ["schema"] = new JsonObject { ["custom"] = "com.pyxis.greenhopper.jira:gh-epic-link" } })
            : new JsonArray(new JsonObject { ["id"] = "summary", ["name"] = "Summary" });
        _jira.Gets["/rest/api/2/search"] = uri =>
        {
            var jql = HttpUtility.ParseQueryString(uri.Query)["jql"]!;
            var keys = _fields.Keys.Where(k => jql.Contains($"\"{k}\"", StringComparison.OrdinalIgnoreCase));
            return new JsonObject
            {
                ["issues"] = new JsonArray(keys.Select(k => (JsonNode)new JsonObject { ["key"] = k, ["fields"] = _fields[k].DeepClone() }).ToArray()),
            };
        };
        _issues = new JiraIssueLookup(new JiraClient(_jira.Client()), _clock);
    }

    private static JsonObject Type(string name, bool subtask = false, int? level = null)
    {
        var t = new JsonObject { ["name"] = name, ["subtask"] = subtask };
        if (level is { } l) t["hierarchyLevel"] = l;
        return t;
    }

    [Fact]
    public async Task Epic_comes_from_the_Epic_Link_field()
    {
        _fields["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };

        var epics = await _issues.GetEpicsAsync(Server, ["ABC-1"]);

        Assert.Equal("BAC-577", epics["ABC-1"]);
        Assert.Contains(_jira.Requests, r => r.Path.Contains("fields=issuetype%2Cparent%2Ccustomfield_10008") || r.Path.Contains("fields=issuetype,parent,customfield_10008"));
    }

    [Fact]
    public async Task Epic_comes_from_the_parent_when_it_is_an_epic()
    {
        _hasEpicLink = false;
        _fields["ABC-1"] = new JsonObject
        {
            ["issuetype"] = Type("Story"),
            ["parent"] = new JsonObject { ["key"] = "BAC-9", ["fields"] = new JsonObject { ["issuetype"] = Type("Epos", level: 1) } },
        };

        Assert.Equal("BAC-9", (await _issues.GetEpicsAsync(Server, ["ABC-1"]))["ABC-1"]);
    }

    [Fact]
    public async Task A_subtask_gets_its_parents_epic()
    {
        _fields["ABC-2"] = new JsonObject
        {
            ["issuetype"] = Type("Sub-task", subtask: true),
            ["parent"] = new JsonObject { ["key"] = "ABC-1", ["fields"] = new JsonObject { ["issuetype"] = Type("Story") } },
        };
        _fields["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };

        var epics = await _issues.GetEpicsAsync(Server, ["ABC-2"]);

        Assert.Equal("BAC-577", epics["ABC-2"]);
        Assert.False(epics.ContainsKey("ABC-1"));
    }

    [Fact]
    public async Task Issues_without_an_epic_and_unknown_issues_get_none()
    {
        _fields["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story") };
        _fields["BAC-577"] = new JsonObject { ["issuetype"] = Type("Epic") }; // an epic has no epic

        var epics = await _issues.GetEpicsAsync(Server, ["ABC-1", "BAC-577", "NOPE-1"]);

        Assert.Null(epics["ABC-1"]);
        Assert.Null(epics["BAC-577"]);
        Assert.Null(epics["NOPE-1"]);
    }

    [Fact]
    public async Task One_unknown_key_does_not_hide_the_others()
    {
        _fields["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };
        _jira.FailWhen = uri => uri.AbsolutePath.EndsWith("/search") && uri.Query.Contains("NOPE-1") ? HttpStatusCode.BadRequest : null;

        var epics = await _issues.GetEpicsAsync(Server, ["ABC-1", "NOPE-1"]);

        Assert.Equal("BAC-577", epics["ABC-1"]);
        Assert.Null(epics["NOPE-1"]);
    }

    [Fact]
    public async Task Epics_are_cached_per_key()
    {
        _fields["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };
        await _issues.GetEpicsAsync(Server, ["ABC-1"]);
        var asked = _jira.Requests.Count;

        await _issues.GetEpicsAsync(Server, ["abc-1"]);
        Assert.Equal(asked, _jira.Requests.Count);

        _clock.Advance(JiraIssueLookup.CacheFor);
        await _issues.GetEpicsAsync(Server, ["ABC-1"]);
        Assert.Single(_jira.Requests.Skip(asked)); // the field list is kept longer
    }

    [Fact]
    public async Task Jira_errors_are_thrown()
    {
        _jira.FailWith = HttpStatusCode.ServiceUnavailable;
        await Assert.ThrowsAsync<JiraException>(() => _issues.GetEpicsAsync(Server, ["ABC-1"]));
    }
}
