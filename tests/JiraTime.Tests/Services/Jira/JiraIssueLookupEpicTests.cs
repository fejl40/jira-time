using System.Net;
using System.Text.Json.Nodes;
using JiraTime.Domain;
using JiraTime.Services.Jira;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services.Jira;

/// <summary>
/// <see cref="JiraIssueLookup"/>: the epic of each issue, for the export.
/// The lookup's tests are split by what it looks up (sprint issues, period issues and statuses, epics), as each needs other issues in FakeJira.
/// </summary>
public class JiraIssueLookupEpicTests : IDisposable
{
    private static readonly JiraConnection Server = TimeHarness.Server;

    private readonly TimeHarness _h = new();
    private bool _hasEpicLink = true;

    public JiraIssueLookupEpicTests() =>
        _h.Jira.Gets["/rest/api/2/field"] = _ => _hasEpicLink
            ? new JsonArray(
                new JsonObject { ["id"] = "summary", ["name"] = "Summary" },
                new JsonObject { ["id"] = "customfield_10008", ["name"] = "Epic Link", ["schema"] = new JsonObject { ["custom"] = "com.pyxis.greenhopper.jira:gh-epic-link" } })
            : new JsonArray(new JsonObject { ["id"] = "summary", ["name"] = "Summary" });

    public void Dispose() => _h.Dispose();

    private static JsonObject Type(string name, bool subtask = false, int? level = null)
    {
        var t = new JsonObject { ["name"] = name, ["subtask"] = subtask };
        if (level is { } l) t["hierarchyLevel"] = l;
        return t;
    }

    [Fact]
    public async Task Epic_comes_from_the_Epic_Link_field()
    {
        _h.Jira.Issues["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };

        var epics = await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]);

        Assert.Equal("BAC-577", epics["ABC-1"]);
        Assert.Contains(_h.Jira.Searches, r => r.Path.Contains("fields=issuetype%2Cparent%2Ccustomfield_10008") || r.Path.Contains("fields=issuetype,parent,customfield_10008"));
    }

    [Fact]
    public async Task Epic_comes_from_the_parent_when_it_is_an_epic()
    {
        _hasEpicLink = false;
        _h.Jira.Issues["ABC-1"] = new JsonObject
        {
            ["issuetype"] = Type("Story"),
            ["parent"] = new JsonObject { ["key"] = "BAC-9", ["fields"] = new JsonObject { ["issuetype"] = Type("Epos", level: 1) } },
        };

        Assert.Equal("BAC-9", (await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]))["ABC-1"]);
    }

    [Fact]
    public async Task A_subtask_gets_its_parents_epic()
    {
        _h.Jira.Issues["ABC-2"] = new JsonObject
        {
            ["issuetype"] = Type("Sub-task", subtask: true),
            ["parent"] = new JsonObject { ["key"] = "ABC-1", ["fields"] = new JsonObject { ["issuetype"] = Type("Story") } },
        };
        _h.Jira.Issues["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };

        var epics = await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-2"]);

        Assert.Equal("BAC-577", epics["ABC-2"]);
        Assert.False(epics.ContainsKey("ABC-1"));
    }

    [Fact]
    public async Task Issues_without_an_epic_and_unknown_issues_get_none()
    {
        _h.Jira.Issues["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story") };
        _h.Jira.Issues["BAC-577"] = new JsonObject { ["issuetype"] = Type("Epic") }; // an epic has no epic

        var epics = await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1", "BAC-577", "NOPE-1"]);

        Assert.Null(epics["ABC-1"]);
        Assert.Null(epics["BAC-577"]);
        Assert.Null(epics["NOPE-1"]);
    }

    [Fact]
    public async Task One_unknown_key_does_not_hide_the_others()
    {
        _h.Jira.Issues["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };

        var epics = await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1", "NOPE-1"]);

        Assert.Equal("BAC-577", epics["ABC-1"]);
        Assert.Null(epics["NOPE-1"]);
    }

    [Fact]
    public async Task Epics_are_cached_per_key()
    {
        _h.Jira.Issues["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };
        await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]);
        var asked = _h.Jira.Requests.Count;

        await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["abc-1"]);
        Assert.Equal(asked, _h.Jira.Requests.Count);

        _h.Clock.Advance(JiraIssueLookup.CacheFor);
        await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]);
        Assert.Single(_h.Jira.Requests.Skip(asked)); // the field list is kept longer
    }

    [Fact]
    public async Task The_Epic_Link_field_is_kept_for_twelve_hours()
    {
        _h.Jira.Issues["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };
        await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]);

        _h.Clock.Advance(JiraIssueLookup.EpicLinkFieldCacheFor - TimeSpan.FromMinutes(1));
        await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]);
        Assert.Single(_h.Jira.Requests, r => r.Path.EndsWith("/field"));

        _h.Clock.Advance(TimeSpan.FromMinutes(1));
        await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-2"]);
        Assert.Equal(2, _h.Jira.Requests.Count(r => r.Path.EndsWith("/field")));
    }

    [Fact]
    public async Task Two_users_on_the_same_Jira_each_get_the_epics_Jira_gives_them()
    {
        var annasProfile = new JiraProfile("JIRAUSER2", "anna", "Anna", null);
        _h.Jira.Issues["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-577" };
        var carls = await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]);
        _h.Jira.Issues["ABC-1"] = new JsonObject { ["issuetype"] = Type("Story"), ["customfield_10008"] = "BAC-9" };

        var annas = await _h.Issues.GetEpicsAsync(Server, annasProfile, ["ABC-1"]);
        var carlsAgain = await _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]);

        Assert.Equal("BAC-577", carls["ABC-1"]);
        Assert.Equal("BAC-9", annas["ABC-1"]); // asked Jira, not Carl's cached answer
        Assert.Equal("BAC-577", carlsAgain["ABC-1"]);
        Assert.Equal(2, _h.Jira.Requests.Count(r => r.Path.EndsWith("/field"))); // each user asks for the field too
    }

    [Fact]
    public async Task Jira_errors_are_thrown()
    {
        _h.Jira.FailWith = HttpStatusCode.ServiceUnavailable;
        await Assert.ThrowsAsync<JiraException>(() => _h.Issues.GetEpicsAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]));
    }
}
