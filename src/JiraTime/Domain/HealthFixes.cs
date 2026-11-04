using System.Globalization;
using System.Net;

namespace JiraTime.Domain;

/// <summary>
/// What a health check's answer means for the user: the fix sentence for a failed request or a missing permission,
/// and the rules for the time zone and the clock.
/// </summary>
public static class HealthFixes
{
    /// <summary>How far this server's clock may be off Jira's before today or a worklog's day can come out wrong.</summary>
    public static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    /// <summary>The start time Jira Time gives every worklog.</summary>
    public static readonly TimeOnly WorklogStart = new(9, 0);

    /// <summary>
    /// The fix for a request that failed: <paramref name="status"/> is Jira's answer, null when Jira couldn't be reached,
    /// and <paramref name="message"/> what went wrong.
    /// </summary>
    public static string ForFailure(HealthCheck check, HttpStatusCode? status, string message) => status switch
    {
        null => $"{message} Check the Jira URL and your network, then run the check again.",
        HttpStatusCode.Unauthorized => "Jira rejected the token. It may be wrong or expired: make a new one in Jira and use Change connection.",
        HttpStatusCode.TooManyRequests => "Jira asked us to slow down. Wait a minute and run the check again.",
        HttpStatusCode.Forbidden => Forbidden(check),
        HttpStatusCode.NotFound when check == HealthCheck.Token => "Jira has no REST API at this URL. Use Change connection and enter Jira's base URL.",
        _ => $"{message} Run the check again; if it keeps failing, look at the request on the Logs page.",
    };

    private static string Forbidden(HealthCheck check) => check switch
    {
        HealthCheck.Token => "Jira refused the token (403). Sign in to Jira in your browser once (it may want a CAPTCHA), then run the check again.",
        HealthCheck.Worklogs or HealthCheck.IssuePicker => "Jira doesn't let your user search issues (403). Ask a Jira admin for Browse Projects on your projects.",
        _ => "Jira refused the request (403). Ask a Jira admin to check your user's permissions.",
    };

    /// <summary>The outcome of the permission check: may the user log work on <paramref name="issueKey"/> (on any issue when null)?</summary>
    public static HealthResult LoggingTime(bool mayLogWork, string? issueKey)
    {
        var where = issueKey is null ? "your issues" : issueKey;
        return mayLogWork
            ? HealthResult.Passed(HealthCheck.LoggingTime, $"You may log work on {where}. Nothing was written.")
            : HealthResult.Failed(HealthCheck.LoggingTime,
                $"Your Jira user may not log work on {where}. Ask a Jira admin for the Work On Issues permission in its project.");
    }

    /// <summary>
    /// Do worklogs, started at 09:00 in <paramref name="worklogZone"/>, fall on the same day in the time zone of the user's
    /// Jira profile (<paramref name="jiraZoneId"/>, unknown when null)? Jira shows and searches them by that day.
    /// </summary>
    /// <param name="fromBrowser">Whether <paramref name="worklogZone"/> is the browser's; else it is the server's.</param>
    public static HealthResult TimeZone(TimeZoneInfo worklogZone, bool fromBrowser, string? jiraZoneId, DateOnly today)
    {
        var whose = fromBrowser ? "your browser's time zone" : "this server's time zone, as your browser didn't say its own";
        var starts = $"Worklogs start at 09:00 in {worklogZone.Id} ({whose})";
        if (jiraZoneId is null || !TimeZoneInfo.TryFindSystemTimeZoneById(jiraZoneId, out var jiraZone))
            return HealthResult.Passed(HealthCheck.TimeZone, starts + ".");

        var start = new DateTimeOffset(today.ToDateTime(WorklogStart), worklogZone.GetUtcOffset(today.ToDateTime(WorklogStart)));
        var inJira = TimeZoneInfo.ConvertTime(start, jiraZone);
        var time = inJira.ToString("HH:mm", CultureInfo.InvariantCulture);
        var jiraDay = DateOnly.FromDateTime(inJira.DateTime);
        if (jiraDay != today)
            return HealthResult.Failed(HealthCheck.TimeZone,
                $"{starts}, which is {time} {(jiraDay < today ? "the day before" : "the day after")} in your Jira profile's time zone ({jiraZone.Id}). " +
                $"Set your Jira profile's time zone to {worklogZone.Id} so Jira shows your work on the right day.");

        var sameZone = SameZone(worklogZone, jiraZone, start);
        return HealthResult.Passed(HealthCheck.TimeZone, sameZone
            ? $"{starts}, the same as your Jira profile."
            : $"{starts}: {time} the same day in your Jira profile's time zone ({jiraZone.Id}).");
    }

    private static bool SameZone(TimeZoneInfo a, TimeZoneInfo b, DateTimeOffset at) =>
        a.Id == b.Id || a.GetUtcOffset(at) == b.GetUtcOffset(at);

    /// <summary>Does this server's clock (<paramref name="serverNow"/>) agree with Jira's (<paramref name="jiraNow"/>)?</summary>
    /// <param name="today">Today on the user's clock, named when it passes.</param>
    public static HealthResult Clock(DateTimeOffset serverNow, DateTimeOffset jiraNow, DateOnly today)
    {
        var off = serverNow - jiraNow;
        var day = today.ToString("dddd d MMMM", CultureInfo.InvariantCulture);
        if (off.Duration() <= ClockTolerance)
            return HealthResult.Passed(HealthCheck.Clock, $"This server's clock agrees with Jira's. Today is {day}.");

        var minutes = (int)Math.Round(off.Duration().TotalMinutes);
        var amount = minutes >= 120 ? $"{minutes / 60} hours" : $"{minutes} minutes";
        var direction = off > TimeSpan.Zero ? "ahead of" : "behind";
        return HealthResult.Failed(HealthCheck.Clock,
            $"This server's clock is {amount} {direction} Jira's. Set the server to get its time automatically, so today and each worklog's day come out right.");
    }
}
