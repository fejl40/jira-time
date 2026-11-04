using System.Net;
using System.Text.Json.Nodes;
using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services;

/// <summary>The health check on Settings, run against the fake Jira: what passes, what fails with which fix, and that it only reads.</summary>
public class JiraHealthTests : IDisposable
{
    private const string Permissions = "/rest/api/2/mypermissions";
    private readonly TimeHarness _h = new(TimeHarness.Server with { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen") });
    private bool _mayLogWork = true;

    public JiraHealthTests()
    {
        _h.Jira.Myself = new { key = "JIRAUSER1", name = "carl", displayName = "Carl Lilholm", timeZone = "Europe/Copenhagen" };
        _h.Jira.Gets[Permissions] = _ => new JsonObject
        {
            ["permissions"] = new JsonObject { ["WORK_ON_ISSUES"] = new JsonObject { ["key"] = "WORK_ON_ISSUES", ["havePermission"] = _mayLogWork } },
        };
        _h.Jira.Gets["/rest/api/2/serverInfo"] = _ => new { serverTime = _h.Clock.Now.ToOffset(TimeSpan.FromHours(1)).ToString("yyyy-MM-dd'T'HH:mm:ss.fff+0100") };
        _h.Jira.Gets["/rest/api/2/issue/picker"] = _ => new { sections = new[] { new { issues = new[] { new { key = "ABC-1", summaryText = "Build login" } } } } };
    }

    public void Dispose() => _h.Dispose();

    private async Task<HealthReport> RunAsync()
    {
        var report = new HealthReport();
        await _h.Get<JiraHealth>().RunAsync(report, () => Task.CompletedTask);
        return report;
    }

    [Fact]
    public async Task Every_check_passes_against_a_healthy_Jira()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-26T09:00:00.000+0100", 5400);

        var report = await RunAsync();

        Assert.All(report.Results, r => Assert.Equal(HealthState.Passed, r.State));
        Assert.Equal("Jira accepts the token. It belongs to Carl Lilholm.", report[HealthCheck.Token].Detail);
        Assert.Equal("Found 1 worklog on 1 issue in the last 7 days, 1.5 h in all.", report[HealthCheck.Worklogs].Detail);
        Assert.Equal("You may log work on ABC-1. Nothing was written.", report[HealthCheck.LoggingTime].Detail);
        Assert.Contains("the same as your Jira profile", report[HealthCheck.TimeZone].Detail);
        Assert.Contains("agrees with Jira's", report[HealthCheck.Clock].Detail);
    }

    [Fact]
    public async Task Nothing_is_written_to_Jira()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-26T09:00:00.000+0100", 5400);

        await RunAsync();

        Assert.All(_h.Jira.Requests, r => Assert.Equal(HttpMethod.Get, r.Method));
        Assert.Contains(_h.Jira.Requests, r => r.Path.StartsWith(Permissions + "?permissions=WORK_ON_ISSUES&issueKey=ABC-1"));
    }

    [Fact]
    public async Task Every_request_is_logged_under_the_health_check_action()
    {
        await RunAsync();

        var log = await _h.RequestLog.ReadDayAsync(TestUsers.Carl.Id, _h.Today);
        Assert.Equal(_h.Jira.Requests.Count, log.Count);
        Assert.All(log, e => Assert.Equal("Health check", e.Action));
        Assert.Single(log.Select(e => e.ActionId).Distinct());
    }

    [Fact]
    public async Task Each_check_shows_running_then_finished_in_order()
    {
        var seen = new List<string>();
        var report = new HealthReport();

        await _h.Get<JiraHealth>().RunAsync(report, () =>
        {
            var finished = report.Results.Count(r => r.IsFinished);
            var running = report.Results.SingleOrDefault(r => r.State == HealthState.Running);
            seen.Add($"{finished} {running?.Check}".Trim());
            return Task.CompletedTask;
        });

        Assert.Equal(["0 Token", "1", "1 Worklogs", "2", "2 IssuePicker", "3", "3 LoggingTime", "4", "4 TimeZone", "5", "5 Clock", "6"], seen);
    }

    [Fact]
    public async Task A_rejected_token_fails_the_token_check_and_skips_the_rest_without_asking_Jira()
    {
        _h.Jira.FailWith = HttpStatusCode.Unauthorized;

        var report = await RunAsync();

        Assert.Equal(HealthState.Failed, report[HealthCheck.Token].State);
        Assert.StartsWith("Jira rejected the token.", report[HealthCheck.Token].Detail);
        Assert.All(report.Results.Skip(1), r => Assert.Equal(HealthState.Skipped, r.State));
        Assert.Single(_h.Jira.Requests);
    }

    [Fact]
    public async Task A_missing_permission_to_log_work_fails_with_the_fix()
    {
        _mayLogWork = false;

        var report = await RunAsync();

        Assert.Equal(HealthState.Failed, report[HealthCheck.LoggingTime].State);
        Assert.Contains("Work On Issues permission", report[HealthCheck.LoggingTime].Detail);
        Assert.Equal(HealthState.Passed, report[HealthCheck.Clock].State);
    }

    [Fact]
    public async Task A_refused_worklog_search_fails_that_check_and_the_others_still_run()
    {
        _h.Jira.FailWhen = uri => uri.Query.Contains("worklogAuthor") ? HttpStatusCode.Forbidden : null;

        var report = await RunAsync();

        Assert.Equal(HealthState.Failed, report[HealthCheck.Worklogs].State);
        Assert.Contains("Browse Projects", report[HealthCheck.Worklogs].Detail);
        Assert.Equal(HealthState.Passed, report[HealthCheck.IssuePicker].State);
        Assert.Equal(HealthState.Passed, report[HealthCheck.Clock].State);
    }

    [Fact]
    public async Task A_Jira_profile_in_another_time_zone_that_moves_the_day_fails()
    {
        _h.Jira.Myself = new { key = "JIRAUSER1", name = "carl", displayName = "Carl Lilholm", timeZone = "Pacific/Honolulu" };

        var report = await RunAsync();

        Assert.Equal(HealthState.Failed, report[HealthCheck.TimeZone].State);
        Assert.Contains("Set your Jira profile's time zone to Europe/Copenhagen", report[HealthCheck.TimeZone].Detail);
    }

    [Fact]
    public async Task A_server_clock_far_from_Jiras_fails()
    {
        _h.Jira.Gets["/rest/api/2/serverInfo"] = _ => new { serverTime = _h.Clock.Now.AddMinutes(-20).ToString("yyyy-MM-dd'T'HH:mm:ss.fff+0000") };

        var report = await RunAsync();

        Assert.Equal(HealthState.Failed, report[HealthCheck.Clock].State);
        Assert.StartsWith("This server's clock is 20 minutes ahead of Jira's.", report[HealthCheck.Clock].Detail);
    }

    [Fact]
    public async Task Jira_out_of_reach_fails_the_token_check_with_the_reason()
    {
        _h.Jira.Offline = true;

        var report = await RunAsync();

        Assert.Equal(HealthState.Failed, report[HealthCheck.Token].State);
        Assert.Contains("Could not reach jira.example.com", report[HealthCheck.Token].Detail);
        Assert.Contains("Check the Jira URL", report[HealthCheck.Token].Detail);
    }

    [Fact]
    public async Task A_rate_limit_is_waited_out_as_for_every_Jira_request()
    {
        _h.Jira.TooManyRequests = 1;
        _h.Jira.RetryAfter = "2";

        var report = await RunAsync();

        Assert.Equal(HealthState.Passed, report[HealthCheck.Token].State);
        Assert.Equal(2, _h.Jira.Requests.Count(r => r.Path.EndsWith("/myself")));
    }
}
