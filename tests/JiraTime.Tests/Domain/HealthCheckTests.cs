using JiraTime.Domain;

namespace JiraTime.Tests.Domain;

public class HealthCheckTests
{
    [Fact]
    public void Checks_run_token_first_then_worklogs_picker_logging_time_time_zone_and_clock()
    {
        Assert.Equal(
            [HealthCheck.Token, HealthCheck.Worklogs, HealthCheck.IssuePicker, HealthCheck.LoggingTime, HealthCheck.TimeZone, HealthCheck.Clock],
            HealthReport.Order);
    }

    [Fact]
    public void A_new_report_has_every_check_waiting()
    {
        var report = new HealthReport();

        Assert.All(report.Results, r => Assert.Equal(HealthState.Waiting, r.State));
        Assert.False(report.IsDone);
        Assert.Equal(0, report.Progress);
    }

    [Fact]
    public void A_failed_token_check_skips_every_check_that_needs_it()
    {
        var report = new HealthReport();
        report.Start(HealthCheck.Token);

        report.Finish(HealthResult.Failed(HealthCheck.Token, "Make a new token."));

        Assert.True(report.IsDone);
        Assert.Equal(HealthState.Failed, report[HealthCheck.Token].State);
        Assert.All(report.Results.Skip(1), r => Assert.Equal(HealthState.Skipped, r.State));
        Assert.All(report.Results.Skip(1), r => Assert.False(report.ShouldRun(r.Check)));
        Assert.Contains("accept the token", report[HealthCheck.Clock].Detail);
        Assert.Equal("1 check needs attention. 5 more couldn't run.", report.Summary());
    }

    [Fact]
    public void Another_failed_check_skips_nothing()
    {
        var report = new HealthReport();
        report.Finish(HealthResult.Passed(HealthCheck.Token, "ok"));

        report.Finish(HealthResult.Failed(HealthCheck.Worklogs, "Ask an admin."));

        Assert.Equal(HealthState.Waiting, report[HealthCheck.IssuePicker].State);
        Assert.True(report.ShouldRun(HealthCheck.IssuePicker));
        Assert.Equal(2.0 / 6, report.Progress, 3);
    }

    [Fact]
    public void A_run_where_everything_passed_says_so()
    {
        var report = new HealthReport();
        foreach (var check in HealthReport.Order) report.Finish(HealthResult.Passed(check, "ok"));

        Assert.True(report.IsDone);
        Assert.Equal(1, report.Progress);
        Assert.Equal("All 6 checks passed. Jira Time has everything it needs.", report.Summary());
    }

    [Fact]
    public void Two_failed_checks_are_counted()
    {
        var report = new HealthReport();
        foreach (var check in HealthReport.Order) report.Finish(HealthResult.Passed(check, "ok"));
        report.Finish(HealthResult.Failed(HealthCheck.TimeZone, "Set it."));
        report.Finish(HealthResult.Failed(HealthCheck.Clock, "Set it."));

        Assert.Equal("2 checks need attention.", report.Summary());
    }

    [Fact]
    public void A_running_check_is_not_finished()
    {
        var report = new HealthReport();

        report.Start(HealthCheck.Token);

        Assert.Equal(HealthState.Running, report[HealthCheck.Token].State);
        Assert.False(report[HealthCheck.Token].IsFinished);
    }

    [Fact]
    public void Every_check_has_a_title_and_a_question()
    {
        Assert.All(HealthReport.Order, c => Assert.NotEmpty(HealthReport.Title(c)));
        Assert.All(HealthReport.Order, c => Assert.Contains("?", HealthReport.Question(c)));
    }
}
