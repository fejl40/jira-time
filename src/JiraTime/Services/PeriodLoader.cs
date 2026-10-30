using System.Globalization;
using JiraTime.Domain;
using JiraTime.Services.Jira;
using JiraTime.Services.JiraLog;
using JiraTime.Services.SignIn;
using JiraTime.Services.Worklogs;

namespace JiraTime.Services;

/// <summary>Where a period that isn't done stands.</summary>
public sealed record OpenPeriod(Period Period, TimeSpan Missing, int NotInJira);

/// <summary>What the timesheet shows around a loaded period.</summary>
/// <param name="JiraError">Why Jira couldn't be read; then only work waiting for Jira is loaded and hours are unknown.</param>
/// <param name="Previous">The period before, when the loaded one is current and the previous isn't done.</param>
/// <param name="Recent">Issues logged on lately, for the picker.</param>
/// <param name="Comments">Recent comments per issue, for the comment fields.</param>
/// <param name="Earlier">Issues that were to do earlier in the period (maybe Done now).</param>
public sealed record PeriodExtras(
    string? JiraError,
    OpenPeriod? Previous,
    IReadOnlyList<JiraIssue> Recent,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Comments,
    IReadOnlyList<IssueWithStatus> Earlier);

/// <summary>
/// Loads a period for the timesheet: logged time (from Jira), days, the previous period's state, and the
/// issues and comments the pickers offer. Jira being down for issue lists just means fewer picks.
/// </summary>
public class PeriodLoader(TimeLog log, WorkCalendarService calendar, JiraIssueLookup issues, CurrentUser user)
{
    /// <summary>Loads the sheet's saved data and sprint, notes the statuses of its issues, and returns what goes around it.</summary>
    public async Task<PeriodExtras> LoadAsync(PeriodSheet sheet, IssueStatuses statuses)
    {
        var p = sheet.Period;
        using var action = JiraAction.Start($"Open period {p.Start.ToString("d MMM", CultureInfo.InvariantCulture)}–{p.End.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}");
        var copyFrom = p.Start.AddDays(-DayFiller.LookBackDays);
        // The three reads don't depend on each other, so they go out together; the Jira client queues what is over its limit.
        var reading = ReadTimeAsync(p, copyFrom, sheet.Today);
        var sprint = OrEmptyAsync((c, profile) => issues.GetSprintAsync(c, profile));
        var earlierInPeriod = OrEmptyAsync((c, profile) => issues.GetPeriodAsync(c, profile, p));
        await Task.WhenAll(reading, sprint, earlierInPeriod);
        var (time, recent) = await reading;
        var earlier = await earlierInPeriod;

        sheet.Load(
            time.Between(p.Start, p.End),
            time.Between(copyFrom, p.Start.AddDays(-1)),
            await calendar.GetDaysAsync(p.Start, p.End),
            time.IsComplete);
        sheet.Sprint = await sprint;
        statuses.Add(sheet.Sprint.Concat(earlier));
        await statuses.LookUpAsync(sheet.Entries.Select(e => e.IssueKey));

        return new PeriodExtras(time.JiraError, await PreviousAsync(sheet, time), RecentWork.Issues(recent), RecentWork.Comments(recent), earlier);
    }

    /// <summary>
    /// Logged time of the period, the days before it to copy from and the period before it, and the recent work
    /// for the pickers. One read covers them all when they are close (a period near today); an old period is
    /// read on its own, so the read doesn't stretch over every day since.
    /// </summary>
    private async Task<(LoggedTime Time, IReadOnlyList<TimeEntry> Recent)> ReadTimeAsync(Period p, DateOnly copyFrom, DateOnly today)
    {
        var from = copyFrom < p.Previous.Start ? copyFrom : p.Previous.Start;
        var recentFrom = today.AddDays(-RecentWork.Days);
        if (recentFrom > p.End.AddDays(1) || today < from.AddDays(-1))
        {
            // Two reads that don't depend on each other: they go out together.
            var period = log.GetRangeAsync(from, p.End);
            var recent = log.GetRecentAsync();
            await Task.WhenAll(period, recent);
            return (await period, await recent);
        }

        var time = await log.GetRangeAsync(from < recentFrom ? from : recentFrom, p.End > today ? p.End : today);
        return (time, time.Between(recentFrom, today));
    }

    /// <summary>The period before the current one, when it has missing hours or work not in Jira.</summary>
    private async Task<OpenPeriod?> PreviousAsync(PeriodSheet sheet, LoggedTime time)
    {
        if (!sheet.Period.Contains(sheet.Today)) return null;
        var prev = sheet.Period.Previous;
        var days = await calendar.GetDaysAsync(prev.Start, prev.End);
        var summary = PeriodSummary.For(prev, days, time.Between(prev.Start, prev.End), sheet.Today, hoursKnown: time.IsComplete);
        return summary.Missing > TimeSpan.Zero || summary.NotInJira > 0 ? new(prev, summary.Missing, summary.NotInJira) : null;
    }

    private async Task<IReadOnlyList<IssueWithStatus>> OrEmptyAsync(Func<JiraConnection, JiraProfile, Task<IReadOnlyList<IssueWithStatus>>> load)
    {
        if (user.Jira is not { } signedIn) return [];
        try
        {
            return await load(signedIn.Connection, signedIn.Profile);
        }
        catch (JiraException)
        {
            return [];
        }
    }
}
