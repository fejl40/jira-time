using System.Net;
using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>
/// <see cref="JiraIssueLookup"/>: the issues assigned to the user in an open sprint.
/// The lookup's tests are split by what it looks up (sprint issues, period issues and statuses, epics), as each needs other issues in FakeJira.
/// </summary>
public class JiraIssueLookupSprintTests : IDisposable
{
    private static readonly JiraConnection Server = TimeHarness.Server;
    private static readonly JiraConnection Cloud = TimeHarness.Cloud;
    private static readonly JiraProfile ServerMe = TestUsers.CarlsProfile;
    private static readonly JiraProfile CloudMe = new("5b10ac8d", null, "Carl", "carl@example.com");

    private readonly TimeHarness _h = new();

    public JiraIssueLookupSprintTests()
    {
        _h.Jira.AddIssue("ABC-1", "Login page", "Story", "In Progress", "indeterminate");
        _h.Jira.AddIssue("ABC-2", "Fix the build", "Task", "To Do", "new");
    }

    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Server_asks_for_my_open_issues_in_open_sprints_by_username()
    {
        var issues = await _h.Issues.GetSprintAsync(Server, ServerMe);

        var req = Assert.Single(_h.Jira.Requests);
        Assert.StartsWith("/rest/api/2/search?", req.Path);
        var jql = FakeJira.Jql(req);
        Assert.Contains("assignee = \"carl\"", jql);
        Assert.Contains("statusCategory != Done", jql);
        Assert.Contains("sprint in openSprints()", jql);
        Assert.Equal(["ABC-1", "ABC-2"], issues.Select(i => i.Key));
    }

    [Fact]
    public async Task Cloud_uses_search_jql_and_the_account_id()
    {
        await _h.Issues.GetSprintAsync(Cloud, CloudMe);

        var req = Assert.Single(_h.Jira.Requests);
        Assert.StartsWith("/rest/api/3/search/jql?", req.Path);
        Assert.Contains("assignee = \"5b10ac8d\"", FakeJira.Jql(req));
    }

    [Fact]
    public async Task Maps_type_status_and_in_progress()
    {
        var issues = await _h.Issues.GetSprintAsync(Server, ServerMe);

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
        _h.Jira.FailWhen = uri => FakeJira.Jql(uri).Contains("openSprints") ? HttpStatusCode.BadRequest : null;

        var issues = await _h.Issues.GetSprintAsync(Server, ServerMe);

        Assert.Equal(2, _h.Jira.Requests.Count);
        Assert.DoesNotContain("openSprints", FakeJira.Jql(_h.Jira.Requests[1]));
        Assert.Contains("assignee = \"carl\"", FakeJira.Jql(_h.Jira.Requests[1]));
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public async Task Other_errors_are_not_swallowed()
    {
        _h.Jira.FailWith = HttpStatusCode.Unauthorized;

        await Assert.ThrowsAsync<JiraException>(() => _h.Issues.GetSprintAsync(Server, ServerMe));
        Assert.Single(_h.Jira.Requests);
    }

    [Fact]
    public async Task Results_are_cached_for_five_minutes()
    {
        await _h.Issues.GetSprintAsync(Server, ServerMe);
        _h.Clock.Advance(TimeSpan.FromMinutes(4));
        await _h.Issues.GetSprintAsync(Server, ServerMe);
        Assert.Single(_h.Jira.Requests);

        _h.Clock.Advance(TimeSpan.FromMinutes(2));
        await _h.Issues.GetSprintAsync(Server, ServerMe);
        Assert.Equal(2, _h.Jira.Requests.Count);
    }

    [Fact]
    public async Task Refresh_bypasses_the_cache_and_profiles_are_cached_separately()
    {
        await _h.Issues.GetSprintAsync(Server, ServerMe);
        await _h.Issues.GetSprintAsync(Server, ServerMe, refresh: true);
        await _h.Issues.GetSprintAsync(Server, ServerMe with { Id = "JIRAUSER2", Username = "anna" });

        Assert.Equal(3, _h.Jira.Requests.Count);
        Assert.Contains("assignee = \"anna\"", FakeJira.Jql(_h.Jira.Requests[2]));
    }
}
