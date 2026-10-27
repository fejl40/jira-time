using System.Net;
using System.Web;
using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

public class PeriodIssuesTests
{
    private static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token", null);
    private static readonly JiraProfile Me = new("JIRAUSER1", "carl", "Carl", null);
    private static readonly Period October2 = Period.For(new DateOnly(2026, 10, 20)); // 16–31 Oct

    private readonly FakeJira _jira = new();
    private readonly Clock _clock = new();
    private readonly JiraIssueLookup _issues;

    /// <summary>What the fake Jira knows: key → (summary, status, category).</summary>
    private readonly Dictionary<string, (string Summary, string Status, string Category)> _known = new()
    {
        ["ABC-1"] = ("Login page", "In Progress", "indeterminate"),
        ["ABC-4"] = ("Old story", "Done", "done"),
    };

    public PeriodIssuesTests()
    {
        _jira.Gets["/rest/api/2/search"] = uri =>
        {
            var jql = Jql(uri);
            var keys = jql.StartsWith("key in")
                ? _known.Keys.Where(k => jql.Contains($"\"{k}\"", StringComparison.OrdinalIgnoreCase)).ToList()
                : _known.Keys.ToList();
            return new
            {
                issues = keys.Select(k => new
                {
                    key = k,
                    fields = new { summary = _known[k].Summary, issuetype = new { name = "Story" }, status = new { name = _known[k].Status, statusCategory = new { key = _known[k].Category } } },
                }),
            };
        };
        _issues = new JiraIssueLookup(new JiraClient(_jira.Client()), _clock);
    }

    private static string Jql(Uri uri) => HttpUtility.ParseQueryString(uri.Query)["jql"]!;
    private static string Jql(FakeJira.Request r) => Jql(new Uri("http://x" + r.Path));

    [Fact]
    public async Task Period_issues_use_Jiras_history_search_for_the_period()
    {
        var issues = await _issues.GetPeriodAsync(Server, Me, October2);

        var jql = Jql(Assert.Single(_jira.Requests));
        Assert.Contains("assignee WAS \"carl\" DURING (\"2026-10-16\", \"2026-10-31\")", jql);
        Assert.Contains("status WAS IN (\"To Do\", \"In Progress\") DURING (\"2026-10-16\", \"2026-10-31\")", jql);
        Assert.DoesNotContain("statusCategory != Done", jql); // Done issues are wanted too
        Assert.Contains(issues, i => i.Key == "ABC-4" && i.IsDone);
    }

    [Fact]
    public async Task Without_those_status_names_it_falls_back_to_issues_not_resolved_before_the_period()
    {
        _jira.FailWhen = uri => Jql(uri).Contains("status WAS IN") ? HttpStatusCode.BadRequest : null;

        var issues = await _issues.GetPeriodAsync(Server, Me, October2);

        Assert.Equal(2, _jira.Requests.Count);
        var jql = Jql(_jira.Requests[1]);
        Assert.Contains("assignee WAS \"carl\" DURING", jql);
        Assert.Contains("resolution IS EMPTY OR resolutiondate >= \"2026-10-16\"", jql);
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public async Task Period_issues_are_cached_per_period()
    {
        await _issues.GetPeriodAsync(Server, Me, October2);
        await _issues.GetPeriodAsync(Server, Me, October2);
        Assert.Single(_jira.Requests);

        await _issues.GetPeriodAsync(Server, Me, October2.Previous);
        Assert.Equal(2, _jira.Requests.Count);
    }

    [Fact]
    public async Task Statuses_are_looked_up_by_key_in_one_query()
    {
        var statuses = await _issues.GetStatusesAsync(Server, ["ABC-4", "abc-1", "ABC-4"]);

        var jql = Jql(Assert.Single(_jira.Requests));
        Assert.StartsWith("key in (", jql);
        Assert.True(statuses["ABC-4"].IsDone);
        Assert.Equal("Done", statuses["ABC-4"].Status);
        Assert.False(statuses["ABC-1"].IsDone);
    }

    [Fact]
    public async Task Statuses_are_cached_including_unknown_keys()
    {
        await _issues.GetStatusesAsync(Server, ["ABC-4", "NOPE-1"]);
        _jira.Requests.Clear();

        var again = await _issues.GetStatusesAsync(Server, ["ABC-4", "NOPE-1"]);

        Assert.Empty(_jira.Requests);
        Assert.Single(again);

        _clock.Advance(JiraIssueLookup.CacheFor);
        await _issues.GetStatusesAsync(Server, ["ABC-4"]);
        Assert.Single(_jira.Requests);
    }

    [Fact]
    public async Task One_unknown_key_does_not_hide_the_others()
    {
        // Jira fails a key-in query that names an issue it doesn't have.
        _jira.FailWhen = uri => Jql(uri).Contains("NOPE-1") ? HttpStatusCode.BadRequest : null;

        var statuses = await _issues.GetStatusesAsync(Server, ["ABC-4", "NOPE-1"]);

        Assert.Equal(["ABC-4"], statuses.Keys);
    }

    [Fact]
    public void Done_means_status_category_done()
    {
        Assert.True(new IssueWithStatus("A-1", "", "Story", "Closed", "done").IsDone);
        Assert.False(new IssueWithStatus("A-1", "", "Story", "To Do", "new").IsDone);
        Assert.False(new IssueWithStatus("A-1", "", "Story", "In Review", "indeterminate").IsDone);
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 27, 8, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
