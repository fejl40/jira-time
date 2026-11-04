using System.Net;
using JiraTime.Domain;

namespace JiraTime.Tests.Domain;

public class HealthFixesTests
{
    private static readonly DateOnly Today = new(2026, 11, 4);
    private static readonly TimeZoneInfo Copenhagen = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    [Fact]
    public void A_rejected_token_says_to_make_a_new_one()
    {
        var fix = HealthFixes.ForFailure(HealthCheck.Worklogs, HttpStatusCode.Unauthorized, "Jira rejected the token (401).");

        Assert.Equal("Jira rejected the token. It may be wrong or expired: make a new one in Jira and use Change connection.", fix);
    }

    [Fact]
    public void Jira_out_of_reach_keeps_the_reason_and_says_to_check_the_URL()
    {
        var fix = HealthFixes.ForFailure(HealthCheck.Token, null, "Could not reach jira.example.com: Connection refused.");

        Assert.StartsWith("Could not reach jira.example.com: Connection refused.", fix);
        Assert.Contains("Check the Jira URL", fix);
    }

    [Theory]
    [InlineData(HealthCheck.Token, "CAPTCHA")]
    [InlineData(HealthCheck.Worklogs, "Browse Projects")]
    [InlineData(HealthCheck.IssuePicker, "Browse Projects")]
    [InlineData(HealthCheck.Clock, "permissions")]
    public void A_refusal_names_what_to_ask_for(HealthCheck check, string expected)
    {
        Assert.Contains(expected, HealthFixes.ForFailure(check, HttpStatusCode.Forbidden, "Forbidden"));
    }

    [Fact]
    public void No_REST_API_at_the_URL_says_to_enter_the_base_URL()
    {
        Assert.Contains("base URL", HealthFixes.ForFailure(HealthCheck.Token, HttpStatusCode.NotFound, "Not found"));
    }

    [Fact]
    public void A_rate_limit_says_to_wait()
    {
        Assert.Contains("Wait a minute", HealthFixes.ForFailure(HealthCheck.Clock, HttpStatusCode.TooManyRequests, "Slow down"));
    }

    [Fact]
    public void Any_other_answer_keeps_Jiras_message_and_points_to_the_logs()
    {
        var fix = HealthFixes.ForFailure(HealthCheck.Worklogs, HttpStatusCode.InternalServerError, "Jira is down.");

        Assert.Equal("Jira is down. Run the check again; if it keeps failing, look at the request on the Logs page.", fix);
    }

    [Fact]
    public void Logging_time_passes_with_the_permission_and_says_nothing_was_written()
    {
        var result = HealthFixes.LoggingTime(true, "ABC-1");

        Assert.Equal(HealthState.Passed, result.State);
        Assert.Equal("You may log work on ABC-1. Nothing was written.", result.Detail);
    }

    [Fact]
    public void A_missing_permission_says_whom_to_ask_for_what()
    {
        var result = HealthFixes.LoggingTime(false, null);

        Assert.Equal(HealthState.Failed, result.State);
        Assert.Equal("Your Jira user may not log work on your issues. Ask a Jira admin for the Work On Issues permission in its project.", result.Detail);
    }

    [Fact]
    public void The_same_time_zone_in_the_browser_and_in_Jira_passes()
    {
        var result = HealthFixes.TimeZone(Copenhagen, true, "Europe/Copenhagen", Today);

        Assert.Equal(HealthState.Passed, result.State);
        Assert.Equal("Worklogs start at 09:00 in Europe/Copenhagen (your browser's time zone), the same as your Jira profile.", result.Detail);
    }

    [Fact]
    public void Another_zone_that_keeps_09_00_on_the_same_day_passes_with_the_time_in_Jira()
    {
        var result = HealthFixes.TimeZone(Copenhagen, true, "America/New_York", Today);

        Assert.Equal(HealthState.Passed, result.State);
        Assert.EndsWith("03:00 the same day in your Jira profile's time zone (America/New_York).", result.Detail);
    }

    [Fact]
    public void A_zone_that_moves_09_00_to_the_day_before_fails_with_the_fix()
    {
        var result = HealthFixes.TimeZone(Copenhagen, true, "Pacific/Honolulu", Today);

        Assert.Equal(HealthState.Failed, result.State);
        Assert.Contains("22:00 the day before in your Jira profile's time zone (Pacific/Honolulu)", result.Detail);
        Assert.EndsWith("Set your Jira profile's time zone to Europe/Copenhagen so Jira shows your work on the right day.", result.Detail);
    }

    [Fact]
    public void Without_the_browsers_zone_or_Jiras_the_check_passes_and_says_which_zone_is_used()
    {
        var result = HealthFixes.TimeZone(TimeZoneInfo.Utc, false, null, Today);

        Assert.Equal(HealthState.Passed, result.State);
        Assert.Contains("this server's time zone, as your browser didn't say its own", result.Detail);
    }

    [Fact]
    public void A_clock_within_five_minutes_of_Jiras_passes_and_names_today()
    {
        var now = new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.Zero);

        var result = HealthFixes.Clock(now, now.AddMinutes(4), Today);

        Assert.Equal(HealthState.Passed, result.State);
        Assert.Equal("This server's clock agrees with Jira's. Today is Wednesday 4 November.", result.Detail);
    }

    [Theory]
    [InlineData(12, "12 minutes ahead of")]
    [InlineData(-12, "12 minutes behind")]
    [InlineData(180, "3 hours ahead of")]
    public void A_clock_further_off_fails_and_says_by_how_much(int minutes, string expected)
    {
        var jira = new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.Zero);

        var result = HealthFixes.Clock(jira.AddMinutes(minutes), jira, Today);

        Assert.Equal(HealthState.Failed, result.State);
        Assert.StartsWith($"This server's clock is {expected} Jira's.", result.Detail);
    }
}
