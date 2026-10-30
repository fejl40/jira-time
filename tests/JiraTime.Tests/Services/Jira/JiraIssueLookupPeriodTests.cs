using System.Net;
using JiraTime.Domain;
using JiraTime.Services.Jira;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services.Jira;

/// <summary>
/// <see cref="JiraIssueLookup"/>: the issues of a period and the current status of issues by key.
/// The lookup's tests are split by what it looks up (sprint issues, period issues and statuses, epics), as each needs other issues in FakeJira.
/// </summary>
public class JiraIssueLookupPeriodTests : IDisposable
{
    private static readonly JiraConnection Server = TimeHarness.Server;
    private static readonly Period October2 = Period.For(new DateOnly(2026, 10, 20)); // 16–31 Oct

    private readonly TimeHarness _h = new();

    public JiraIssueLookupPeriodTests()
    {
        _h.Jira.AddIssue("ABC-1", "Login page", status: "In Progress", category: "indeterminate");
        _h.Jira.AddIssue("ABC-4", "Old story", status: "Done", category: "done");
    }

    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Period_issues_use_Jiras_history_search_for_the_period()
    {
        var issues = await _h.Issues.GetPeriodAsync(Server, TestUsers.CarlsProfile, October2);

        var jql = FakeJira.Jql(Assert.Single(_h.Jira.Requests));
        Assert.Contains("assignee WAS \"carl\" DURING (\"2026-10-16\", \"2026-10-31\")", jql);
        Assert.Contains("status WAS IN (\"To Do\", \"In Progress\") DURING (\"2026-10-16\", \"2026-10-31\")", jql);
        Assert.DoesNotContain("statusCategory != Done", jql); // Done issues are wanted too
        Assert.Contains(issues, i => i.Key == "ABC-4" && i.IsDone);
    }

    [Fact]
    public async Task Without_those_status_names_it_falls_back_to_issues_not_resolved_before_the_period()
    {
        _h.Jira.FailWhen = uri => FakeJira.Jql(uri).Contains("status WAS IN") ? HttpStatusCode.BadRequest : null;

        var issues = await _h.Issues.GetPeriodAsync(Server, TestUsers.CarlsProfile, October2);

        Assert.Equal(2, _h.Jira.Requests.Count);
        var jql = FakeJira.Jql(_h.Jira.Requests[1]);
        Assert.Contains("assignee WAS \"carl\" DURING", jql);
        Assert.Contains("resolution IS EMPTY OR resolutiondate >= \"2026-10-16\"", jql);
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public async Task Period_issues_are_cached_per_period()
    {
        await _h.Issues.GetPeriodAsync(Server, TestUsers.CarlsProfile, October2);
        await _h.Issues.GetPeriodAsync(Server, TestUsers.CarlsProfile, October2);
        Assert.Single(_h.Jira.Requests);

        await _h.Issues.GetPeriodAsync(Server, TestUsers.CarlsProfile, October2.Previous);
        Assert.Equal(2, _h.Jira.Requests.Count);
    }

    [Fact]
    public async Task Statuses_are_looked_up_by_key_in_one_query()
    {
        var statuses = await _h.Issues.GetStatusesAsync(Server, TestUsers.CarlsProfile, ["ABC-4", "abc-1", "ABC-4"]);

        var jql = FakeJira.Jql(Assert.Single(_h.Jira.Requests));
        Assert.StartsWith("key in (", jql);
        Assert.True(statuses["ABC-4"].IsDone);
        Assert.Equal("Done", statuses["ABC-4"].Status);
        Assert.False(statuses["ABC-1"].IsDone);
    }

    [Fact]
    public async Task Statuses_are_cached_including_unknown_keys()
    {
        await _h.Issues.GetStatusesAsync(Server, TestUsers.CarlsProfile, ["ABC-4", "NOPE-1"]);
        _h.Jira.Requests.Clear();

        var again = await _h.Issues.GetStatusesAsync(Server, TestUsers.CarlsProfile, ["ABC-4", "NOPE-1"]);

        Assert.Empty(_h.Jira.Requests);
        Assert.Single(again);

        _h.Clock.Advance(JiraIssueLookup.CacheFor);
        await _h.Issues.GetStatusesAsync(Server, TestUsers.CarlsProfile, ["ABC-4"]);
        Assert.Single(_h.Jira.Requests);
    }

    [Fact]
    public async Task Two_users_on_the_same_Jira_each_get_the_status_Jira_gives_them()
    {
        var annasProfile = new JiraProfile("JIRAUSER2", "anna", "Anna", null);
        var carls = await _h.Issues.GetStatusesAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]);
        _h.Jira.AddIssue("ABC-1", "Login page", status: "Done", category: "done");

        var annas = await _h.Issues.GetStatusesAsync(Server, annasProfile, ["ABC-1"]);
        var carlsAgain = await _h.Issues.GetStatusesAsync(Server, TestUsers.CarlsProfile, ["ABC-1"]);

        Assert.Equal("In Progress", carls["ABC-1"].Status);
        Assert.Equal("Done", annas["ABC-1"].Status); // asked Jira, not Carl's cached answer
        Assert.Equal("In Progress", carlsAgain["ABC-1"].Status); // Carl's own cache still answers him
        Assert.Equal(2, _h.Jira.Requests.Count);
    }

    [Fact]
    public async Task One_unknown_key_does_not_hide_the_others()
    {
        var statuses = await _h.Issues.GetStatusesAsync(Server, TestUsers.CarlsProfile, ["ABC-4", "NOPE-1"]);

        Assert.Equal(["ABC-4"], statuses.Keys);
    }

    [Fact]
    public void Done_means_status_category_done()
    {
        Assert.True(new IssueWithStatus("A-1", "", "Story", "Closed", "done").IsDone);
        Assert.False(new IssueWithStatus("A-1", "", "Story", "To Do", "new").IsDone);
        Assert.False(new IssueWithStatus("A-1", "", "Story", "In Review", "indeterminate").IsDone);
    }
}
