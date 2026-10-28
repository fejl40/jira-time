using JiraTime.Domain;

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
        // One read covers the period, the days before it to copy from, and the period before it.
        var copyFrom = p.Start.AddDays(-DayFiller.LookBackDays);
        var time = await log.GetRangeAsync(new[] { copyFrom, p.Previous.Start }.Min(), p.End);
        sheet.Load(
            time.Between(p.Start, p.End),
            time.Between(copyFrom, p.Start.AddDays(-1)),
            await calendar.GetDaysAsync(p.Start, p.End),
            time.IsComplete);

        var recent = await log.GetRecentAsync();
        sheet.Sprint = await OrEmptyAsync((c, profile) => issues.GetSprintAsync(c, profile));
        var earlier = await OrEmptyAsync((c, profile) => issues.GetPeriodAsync(c, profile, p));
        statuses.Add(sheet.Sprint.Concat(earlier));
        await statuses.LookUpAsync(sheet.Entries.Select(e => e.IssueKey));

        return new PeriodExtras(time.JiraError, await PreviousAsync(sheet, time), RecentWork.Issues(recent), RecentWork.Comments(recent), earlier);
    }

    /// <summary>The period before the current one, when it has missing hours or work not in Jira.</summary>
    private async Task<OpenPeriod?> PreviousAsync(PeriodSheet sheet, LoggedTime time)
    {
        if (!sheet.Period.Contains(sheet.Today)) return null;
        var prev = sheet.Period.Previous;
        var entries = time.Between(prev.Start, prev.End);
        var notInJira = entries.Count(e => !e.IsSynced);
        // Without Jira the hours are unknown, so only work waiting for Jira is worth a reminder.
        var missing = time.IsComplete
            ? PeriodSummary.For(prev, await calendar.GetDaysAsync(prev.Start, prev.End), entries, sheet.Today).Missing
            : TimeSpan.Zero;
        return missing > TimeSpan.Zero || notInJira > 0 ? new(prev, missing, notInJira) : null;
    }

    private async Task<IReadOnlyList<IssueWithStatus>> OrEmptyAsync(Func<JiraConnection, JiraProfile, Task<IReadOnlyList<IssueWithStatus>>> load)
    {
        if (user is not { Connection: { } c, Profile: { } profile }) return [];
        try
        {
            return await load(c, profile);
        }
        catch (JiraException)
        {
            return [];
        }
    }
}
