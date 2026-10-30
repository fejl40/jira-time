using System.Net;
using System.Text.Json.Nodes;
using JiraTime.Services.Jira;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services.Jira;

/// <summary>Searches by key: unknown keys are skipped, long key lists go in batches and long answers in pages.</summary>
public class JiraSearchTests : IDisposable
{
    private readonly TimeHarness _h = new();
    private readonly JiraClient _jira;

    public JiraSearchTests() => _jira = new JiraClient(_h.Http);

    public void Dispose() => _h.Dispose();

    private List<string> AddIssues(int count, int first = 1)
    {
        var keys = Enumerable.Range(first, count).Select(i => $"ABC-{i}").ToList();
        foreach (var key in keys) _h.Jira.AddIssue(key, $"Issue {key}");
        return keys;
    }

    [Fact]
    public async Task A_short_key_search_is_a_GET_that_asks_Jira_to_warn_about_unknown_keys()
    {
        AddIssues(2);

        var issues = await _jira.GetIssuesAsync(TimeHarness.Server, ["ABC-1", "NOPE-1", "ABC-2"]);

        Assert.Equal(["ABC-1", "ABC-2"], issues.Select(i => i.Key));
        var search = Assert.Single(_h.Jira.Requests); // the unknown key doesn't split the search
        Assert.Equal(HttpMethod.Get, search.Method);
        Assert.Contains("validateQuery=warn", search.Path);
    }

    [Fact]
    public async Task A_Jira_that_still_refuses_an_unknown_key_is_asked_each_key_on_its_own()
    {
        using var cloud = new TimeHarness(TimeHarness.Cloud); // Cloud's search has no validateQuery
        cloud.Jira.AddIssue("ABC-1", "Login page");
        cloud.Jira.AddIssue("ABC-2", "Fix the build");

        var issues = await new JiraClient(cloud.Http).GetIssuesAsync(TimeHarness.Cloud, ["ABC-1", "NOPE-1", "ABC-2"]);

        Assert.Equal(["ABC-1", "ABC-2"], issues.Select(i => i.Key));
        Assert.Equal(4, cloud.Jira.Requests.Count);
        Assert.DoesNotContain(cloud.Jira.Requests, r => r.Path.Contains("validateQuery"));
    }

    [Fact]
    public async Task More_than_100_keys_go_in_batches_of_100_and_a_long_search_is_sent_as_POST()
    {
        var keys = AddIssues(250, first: 1001);

        var issues = await _jira.GetIssuesAsync(TimeHarness.Server, keys);

        Assert.Equal(250, issues.Count);
        Assert.Equal([50, 100, 100], _h.Jira.Requests.Select(r => Search(r).Jql.Split(',').Length).Order());
        Assert.All(_h.Jira.Requests, r => Assert.Equal("warn", Search(r).ValidateQuery));
        var posts = _h.Jira.Requests.Where(r => r.Method == HttpMethod.Post).ToList();
        Assert.Equal(2, posts.Count); // the two batches of 100; 50 keys still fit in a URL
        Assert.All(posts, r => Assert.Equal("/rest/api/2/search", r.Path));
    }

    /// <summary>What a recorded search asked for, sent as GET or POST.</summary>
    private static FakeSearch Search(FakeJira.Request r) => FakeSearch.Read(new Uri("http://jira" + r.Path), r.Body);

    [Fact]
    public async Task More_than_100_epics_are_all_read()
    {
        var keys = AddIssues(150);
        foreach (var key in keys) _h.Jira.Issues[key]["parent"] = new JsonObject
        {
            ["key"] = "BAC-1", ["fields"] = new JsonObject { ["issuetype"] = new JsonObject { ["name"] = "Epic" } },
        };

        var epics = await _jira.GetEpicsAsync(TimeHarness.Server, keys, null);

        Assert.Equal(150, epics.Count);
        Assert.All(epics.Values, epic => Assert.Equal("BAC-1", epic));
    }

    [Fact]
    public async Task An_answer_longer_than_a_page_is_read_page_by_page()
    {
        var keys = AddIssues(100);
        _h.Jira.SearchPageLimit = 40;

        var issues = await _jira.GetIssuesAsync(TimeHarness.Server, keys);

        Assert.Equal(100, issues.Count);
        Assert.Equal([0, 40, 80], _h.Jira.Requests.Select(r => Search(r).StartAt));
    }

    [Fact]
    public async Task Cloud_pages_with_the_page_token()
    {
        using var cloud = new TimeHarness(TimeHarness.Cloud);
        var keys = Enumerable.Range(1, 100).Select(i => $"ABC-{i}").ToList();
        foreach (var key in keys) cloud.Jira.AddIssue(key, key);
        cloud.Jira.SearchPageLimit = 40;

        var issues = await new JiraClient(cloud.Http).GetIssuesAsync(TimeHarness.Cloud, keys);

        Assert.Equal(100, issues.Count);
        Assert.Equal([0, 40, 80], cloud.Jira.Requests.Select(r => Search(r).StartAt)); // the fake's page token is where the page starts
        Assert.All(cloud.Jira.Requests, r => Assert.StartsWith("/rest/api/3/search/jql", r.Path));
    }

    [Fact]
    public async Task A_search_that_fails_for_another_reason_is_not_split_up()
    {
        AddIssues(2);
        _h.Jira.FailWith = HttpStatusCode.ServiceUnavailable;

        await Assert.ThrowsAsync<JiraException>(() => _jira.GetIssuesAsync(TimeHarness.Server, ["ABC-1", "ABC-2"]));

        Assert.Single(_h.Jira.Requests);
    }
}
