using System.Net;
using System.Web;
using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

public class SprintIssuesTests
{
    private static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token", null);
    private static readonly JiraConnection Cloud = new("https://x.atlassian.net", JiraAuthType.Cloud, "token", "me@example.com");
    private static readonly JiraProfile ServerMe = new("JIRAUSER1", "carl", "Carl", null);
    private static readonly JiraProfile CloudMe = new("5b10ac8d", null, "Carl", "me@example.com");

    private readonly FakeJira _jira = new();
    private readonly FakeClock _clock = new();
    private readonly JiraIssueLookup _sprint;

    public SprintIssuesTests()
    {
        object Result(Uri _) => new
        {
            issues = new object[]
            {
                Issue("ABC-1", "Login page", "Story", "In Progress", "indeterminate"),
                Issue("ABC-2", "Fix the build", "Task", "To Do", "new"),
            },
        };
        _jira.Gets["/rest/api/2/search"] = Result;
        _jira.Gets["/rest/api/3/search/jql"] = Result;
        _sprint = new JiraIssueLookup(new JiraClient(_jira.Client()), _clock);
    }

    private static object Issue(string key, string summary, string type, string status, string category) => new
    {
        key,
        fields = new { summary, issuetype = new { name = type }, status = new { name = status, statusCategory = new { key = category } } },
    };

    private static string Jql(FakeJira.Request r) =>
        HttpUtility.ParseQueryString(new Uri("http://x" + r.Path).Query)["jql"]!;

    [Fact]
    public async Task Server_asks_for_my_open_issues_in_open_sprints_by_username()
    {
        var issues = await _sprint.GetSprintAsync(Server, ServerMe);

        var req = Assert.Single(_jira.Requests);
        Assert.StartsWith("/rest/api/2/search?", req.Path);
        var jql = Jql(req);
        Assert.Contains("assignee = \"carl\"", jql);
        Assert.Contains("statusCategory != Done", jql);
        Assert.Contains("sprint in openSprints()", jql);
        Assert.Equal(["ABC-1", "ABC-2"], issues.Select(i => i.Key));
    }

    [Fact]
    public async Task Cloud_uses_search_jql_and_the_account_id()
    {
        await _sprint.GetSprintAsync(Cloud, CloudMe);

        var req = Assert.Single(_jira.Requests);
        Assert.StartsWith("/rest/api/3/search/jql?", req.Path);
        Assert.Contains("assignee = \"5b10ac8d\"", Jql(req));
    }

    [Fact]
    public async Task Maps_type_status_and_in_progress()
    {
        var issues = await _sprint.GetSprintAsync(Server, ServerMe);

        var story = issues[0];
        Assert.Equal("Login page", story.Summary);
        Assert.Equal("Story", story.Type);
        Assert.Equal("In Progress", story.Status);
        Assert.True(story.IsInProgress);
        Assert.False(issues[1].IsInProgress);
        Assert.Equal("Task", issues[1].Type);
    }

    [Fact]
    public async Task Falls_back_to_all_my_open_issues_when_sprints_are_unsupported()
    {
        // Jira without Jira Software rejects the sprint field with 400.
        _jira.FailWhen = uri => HttpUtility.UrlDecode(uri.Query).Contains("openSprints") ? HttpStatusCode.BadRequest : null;

        var issues = await _sprint.GetSprintAsync(Server, ServerMe);

        Assert.Equal(2, _jira.Requests.Count);
        Assert.DoesNotContain("openSprints", Jql(_jira.Requests[1]));
        Assert.Contains("assignee = \"carl\"", Jql(_jira.Requests[1]));
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public async Task Other_errors_are_not_swallowed()
    {
        _jira.FailWith = HttpStatusCode.Unauthorized;

        await Assert.ThrowsAsync<JiraException>(() => _sprint.GetSprintAsync(Server, ServerMe));
        Assert.Single(_jira.Requests);
    }

    [Fact]
    public async Task Results_are_cached_for_five_minutes()
    {
        await _sprint.GetSprintAsync(Server, ServerMe);
        _clock.Advance(TimeSpan.FromMinutes(4));
        await _sprint.GetSprintAsync(Server, ServerMe);
        Assert.Single(_jira.Requests);

        _clock.Advance(TimeSpan.FromMinutes(2));
        await _sprint.GetSprintAsync(Server, ServerMe);
        Assert.Equal(2, _jira.Requests.Count);
    }

    [Fact]
    public async Task Refresh_bypasses_the_cache_and_profiles_are_cached_separately()
    {
        await _sprint.GetSprintAsync(Server, ServerMe);
        await _sprint.GetSprintAsync(Server, ServerMe, refresh: true);
        await _sprint.GetSprintAsync(Server, ServerMe with { Id = "JIRAUSER2", Username = "anna" });

        Assert.Equal(3, _jira.Requests.Count);
        Assert.Contains("assignee = \"anna\"", Jql(_jira.Requests[2]));
    }

    private sealed class FakeClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 19, 8, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
