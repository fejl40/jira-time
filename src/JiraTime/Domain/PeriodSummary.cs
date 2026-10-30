namespace JiraTime.Domain;

/// <summary>
/// Logged and expected hours for the part of a week that falls in a period: <see cref="Logged"/> is Jira hours,
/// <see cref="Expected"/> calendar hours (see <see cref="PeriodSummary"/>).
/// </summary>
public record WeekTotal(DateOnly Start, DateOnly End, TimeSpan Logged, TimeSpan Expected)
{
    public int Days => End.DayNumber - Start.DayNumber + 1;
}

/// <summary>
/// Where a period stands, from its days and saved entries. Pure logic. It holds two kinds of hours:
/// <list type="bullet">
/// <item><b>Jira hours</b> (logged, missing, complete) come from the worklogs. When Jira couldn't be read they are
/// unknown: the entries are then only the work waiting for Jira, so nothing counts as missing and the period isn't
/// complete.</item>
/// <item><b>Calendar hours</b> (expected, still to log) come from the user's work hours and days off, so they are
/// always known, with or without Jira.</item>
/// </list>
/// </summary>
public sealed class PeriodSummary
{
    private readonly IReadOnlyDictionary<DateOnly, DayInfo> _days;
    private readonly IReadOnlyDictionary<DateOnly, TimeSpan> _logged;
    private readonly DateOnly _today;

    private PeriodSummary(Period period, IReadOnlyDictionary<DateOnly, DayInfo> days, IReadOnlyList<TimeEntry> entries, DateOnly today, bool jiraHoursKnown)
    {
        var inPeriod = entries.Where(e => period.Contains(e.Date)).ToList();
        (Period, _days, _today, JiraHoursKnown) = (period, days, today, jiraHoursKnown);
        _logged = inPeriod.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => TimeEntry.Total(g));
        NotInJira = inPeriod.Count(e => !e.IsSynced);
    }

    /// <summary>A period's summary; <paramref name="hoursKnown"/> is false when Jira couldn't be read.</summary>
    public static PeriodSummary For(Period period, IReadOnlyDictionary<DateOnly, DayInfo> days, IReadOnlyList<TimeEntry> entries, DateOnly today, bool hoursKnown = true) =>
        new(period, days, entries, today, hoursKnown);

    public Period Period { get; }

    /// <summary>Weeks of the period with their Jira hours (logged) and calendar hours (expected).</summary>
    public IReadOnlyList<WeekTotal> Weeks =>
        Period.Days
            .GroupBy(d => d.AddDays(-(((int)d.DayOfWeek + 6) % 7))) // Monday of the week
            .Select(g => new WeekTotal(g.First(), g.Last(), g.Select(LoggedOn).Sum(), g.Select(ExpectedOn).Sum()))
            .ToList();

    // Jira hours: read from the worklogs, so unknown when Jira couldn't be read.

    /// <summary>False when Jira couldn't be read: the entries are only the work waiting for Jira.</summary>
    public bool JiraHoursKnown { get; }
    /// <summary>Hours saved in the period; only the work waiting for Jira when Jira couldn't be read.</summary>
    public TimeSpan Logged => _logged.Values.Sum();
    /// <summary>Saved entries in the period that Jira doesn't have yet.</summary>
    public int NotInJira { get; }
    /// <summary>Hours short on workdays up to today; zero when Jira couldn't be read.</summary>
    public TimeSpan Missing => Period.Days.Select(MissingOn).Sum();

    /// <summary>Hours saved on a day.</summary>
    public TimeSpan LoggedOn(DateOnly day) => _logged.GetValueOrDefault(day);

    /// <summary>Saved hours still missing on a day; only counted up to today, and zero when Jira couldn't be read.</summary>
    public TimeSpan MissingOn(DateOnly day)
    {
        if (!JiraHoursKnown || day > _today || !_days.TryGetValue(day, out var info)) return TimeSpan.Zero;
        return info.MissingWith(LoggedOn(day));
    }

    /// <summary>Jira was read, nothing is short up to today, nothing is unsaved and everything is in Jira.</summary>
    public bool IsComplete(bool hasUnsaved) => JiraHoursKnown && Missing == TimeSpan.Zero && NotInJira == 0 && !hasUnsaved;

    // Calendar hours: from the user's work hours and days off, so known with or without Jira.

    /// <summary>Hours a day should have (zero on weekends, holidays, days off and days that weren't loaded).</summary>
    public TimeSpan ExpectedOn(DateOnly day) => _days.TryGetValue(day, out var info) ? info.Expected : TimeSpan.Zero;

    /// <summary>Hours expected on the workdays after today, whatever is logged.</summary>
    public TimeSpan StillToLog => Period.Days.Where(d => d > _today).Select(ExpectedOn).Sum();

    /// <summary>True in the last <paramref name="days"/> days of the period that contains today.</summary>
    public static bool EndsSoon(Period period, DateOnly today, int days = 3) =>
        period.Contains(today) && period.End.DayNumber - today.DayNumber < days;
}
