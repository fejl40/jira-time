using System.Globalization;
using JiraTime.Domain;
using JiraTime.Services.Jira;
using JiraTime.Services.JiraLog;
using JiraTime.Services.SignIn;

namespace JiraTime.Services;

/// <summary>
/// Runs the health check on Settings: asks Jira, one check after the other, whether everything the app needs works.
/// Only reads; nothing is written to Jira. Every request goes through <see cref="JiraHttp"/> (rate limits, request log)
/// under the Health check action.
/// </summary>
public sealed class JiraHealth(CurrentUser user, JiraClient issues, JiraWorklogClient worklogs, TimeProvider clock)
{
    /// <summary>The action the health check's requests are logged under.</summary>
    public const string ActionName = "Health check";

    /// <summary>What earlier checks found that later ones use.</summary>
    private sealed class Found
    {
        public JiraProfile? Profile;
        public string? JiraTimeZone;
        public string? IssueKey;
    }

    /// <summary>
    /// Runs every check into <paramref name="report"/>, in order; <paramref name="changed"/> is called each time a check
    /// starts or finishes, so the page can show it.
    /// </summary>
    public async Task RunAsync(HealthReport report, Func<Task> changed, CancellationToken ct = default)
    {
        using var action = JiraAction.Start(ActionName);
        var jira = user.RequireJira().Connection;
        var found = new Found();
        foreach (var check in HealthReport.Order)
        {
            if (!report.ShouldRun(check)) continue;
            report.Start(check);
            await changed();
            report.Finish(await RunOneAsync(check, jira, found, ct));
            await changed();
        }
    }

    private async Task<HealthResult> RunOneAsync(HealthCheck check, JiraConnection jira, Found found, CancellationToken ct)
    {
        try
        {
            return check switch
            {
                HealthCheck.Token => await TokenAsync(jira, found, ct),
                HealthCheck.Worklogs => await WorklogsAsync(jira, found, ct),
                HealthCheck.IssuePicker => await IssuePickerAsync(jira, found, ct),
                HealthCheck.LoggingTime => HealthFixes.LoggingTime(await issues.MayLogWorkAsync(jira, found.IssueKey, ct), found.IssueKey),
                HealthCheck.TimeZone => TimeZone(jira, found),
                _ => await ClockAsync(jira, ct),
            };
        }
        catch (JiraException ex)
        {
            return HealthResult.Failed(check, HealthFixes.ForFailure(check, ex.Status, ex.Message));
        }
    }

    private async Task<HealthResult> TokenAsync(JiraConnection jira, Found found, CancellationToken ct)
    {
        (found.Profile, found.JiraTimeZone) = await issues.GetMyselfAndTimeZoneAsync(jira, ct);
        return HealthResult.Passed(HealthCheck.Token, $"Jira accepts the token. It belongs to {found.Profile.DisplayName}.");
    }

    private async Task<HealthResult> WorklogsAsync(JiraConnection jira, Found found, CancellationToken ct)
    {
        var today = clock.Today(user.TimeZone);
        var lastWeek = await worklogs.GetMyWorklogsAsync(jira, Profile(found), today.AddDays(-6), today, ct: ct);
        found.IssueKey ??= lastWeek.Select(w => w.IssueKey).FirstOrDefault();
        if (lastWeek.Count == 0)
            return HealthResult.Passed(HealthCheck.Worklogs, "The search works. You have no worklogs in the last 7 days.");
        var issueCount = lastWeek.Select(w => w.IssueKey).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var hours = lastWeek.Select(w => w.Duration).Sum().TotalHours.ToString("0.##", CultureInfo.InvariantCulture);
        return HealthResult.Passed(HealthCheck.Worklogs,
            $"Found {Plural(lastWeek.Count, "worklog")} on {Plural(issueCount, "issue")} in the last 7 days, {hours} h in all.");
    }

    private async Task<HealthResult> IssuePickerAsync(JiraConnection jira, Found found, CancellationToken ct)
    {
        var sprint = await issues.GetSprintIssuesAsync(jira, Profile(found), ct);
        found.IssueKey ??= sprint.Select(i => i.Key).FirstOrDefault();
        var searched = await issues.SearchIssuesAsync(jira, found.IssueKey ?? "a", ct);
        return HealthResult.Passed(HealthCheck.IssuePicker,
            $"{Plural(sprint.Count, "issue")} assigned to you and open; Jira search found {Plural(searched.Count, "issue")}.");
    }

    private HealthResult TimeZone(JiraConnection jira, Found found) =>
        HealthFixes.TimeZone(jira.TimeZone ?? clock.LocalTimeZone, jira.TimeZone is not null, found.JiraTimeZone, clock.Today(user.TimeZone));

    private async Task<HealthResult> ClockAsync(JiraConnection jira, CancellationToken ct)
    {
        var jiraNow = await issues.GetServerTimeAsync(jira, ct);
        var today = clock.Today(user.TimeZone);
        return jiraNow is { } time
            ? HealthFixes.Clock(clock.GetUtcNow(), time, today)
            : HealthResult.Failed(HealthCheck.Clock, "Jira didn't say what time it is, so the clock couldn't be compared. Run the check again later.");
    }

    private static JiraProfile Profile(Found found) =>
        found.Profile ?? throw new InvalidOperationException("The token check runs first.");

    private static string Plural(int n, string word) => $"{n} {word}{(n == 1 ? "" : "s")}";
}
