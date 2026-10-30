namespace JiraTime.Domain;

/// <summary>Logged and expected hours for the part of a week that falls in a period.</summary>
public record WeekTotal(DateOnly Start, DateOnly End, TimeSpan Logged, TimeSpan Expected)
{
    public int Days => End.DayNumber - Start.DayNumber + 1;
}

/// <summary>
/// Where a period stands: hours logged, missing so far, still to log, per week and per day, and work Jira doesn't
/// have yet. Pure logic over the period's days and saved entries. When Jira couldn't be read the hours are unknown:
/// the entries are then only the work waiting for Jira, so nothing counts as missing and the period isn't complete.
/// </summary>
public sealed class PeriodSummary
{
    private readonly IReadOnlyDictionary<DateOnly, DayInfo> _days;
    private readonly IReadOnlyDictionary<DateOnly, TimeSpan> _logged;
    private readonly DateOnly _today;

    private PeriodSummary(Period period, IReadOnlyDictionary<DateOnly, DayInfo> days, IReadOnlyList<TimeEntry> entries, DateOnly today, bool hoursKnown)
    {
        var inPeriod = entries.Where(e => period.Contains(e.Date)).ToList();
        (Period, _days, _today, HoursKnown) = (period, days, today, hoursKnown);
        _logged = inPeriod.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => TimeEntry.Total(g));
        NotInJira = inPeriod.Count(e => !e.IsSynced);
    }

    public Period Period { get; }
    /// <summary>False when Jira couldn't be read and the entries are only the work waiting for Jira.</summary>
    public bool HoursKnown { get; }
    public TimeSpan Logged => _logged.Values.Sum();
    /// <summary>Saved entries in the period that Jira doesn't have yet.</summary>
    public int NotInJira { get; }
    /// <summary>Hours short on workdays up to today; zero when the hours are unknown.</summary>
    public TimeSpan Missing => Period.Days.Select(MissingOn).Sum();
    /// <summary>Hours expected on the workdays after today.</summary>
    public TimeSpan StillToLog => Period.Days.Where(d => d > _today).Select(ExpectedOn).Sum();

    public IReadOnlyList<WeekTotal> Weeks =>
        Period.Days
            .GroupBy(d => d.AddDays(-(((int)d.DayOfWeek + 6) % 7))) // Monday of the week
            .Select(g => new WeekTotal(g.First(), g.Last(), g.Select(LoggedOn).Sum(), g.Select(ExpectedOn).Sum()))
            .ToList();

    public static PeriodSummary For(Period period, IReadOnlyDictionary<DateOnly, DayInfo> days, IReadOnlyList<TimeEntry> entries, DateOnly today, bool hoursKnown = true) =>
        new(period, days, entries, today, hoursKnown);

    /// <summary>Hours saved on a day.</summary>
    public TimeSpan LoggedOn(DateOnly day) => _logged.GetValueOrDefault(day);

    /// <summary>Hours a day should have (zero on weekends, holidays, days off and days that weren't loaded).</summary>
    public TimeSpan ExpectedOn(DateOnly day) => _days.TryGetValue(day, out var info) ? info.Expected : TimeSpan.Zero;

    /// <summary>Saved hours still missing on a day; only counted up to today, and zero when the hours are unknown.</summary>
    public TimeSpan MissingOn(DateOnly day)
    {
        if (!HoursKnown || day > _today || !_days.TryGetValue(day, out var info)) return TimeSpan.Zero;
        return info.MissingWith(LoggedOn(day));
    }

    /// <summary>Hours are known, nothing is short up to today, nothing is unsaved and everything is in Jira.</summary>
    public bool IsComplete(bool hasUnsaved) => HoursKnown && Missing == TimeSpan.Zero && NotInJira == 0 && !hasUnsaved;

    /// <summary>True in the last <paramref name="days"/> days of the period that contains today.</summary>
    public static bool EndsSoon(Period period, DateOnly today, int days = 3) =>
        period.Contains(today) && period.End.DayNumber - today.DayNumber < days;
}
