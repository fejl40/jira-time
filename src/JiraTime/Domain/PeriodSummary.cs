namespace JiraTime.Domain;

/// <summary>Logged and expected hours for the part of a week that falls in a period.</summary>
public record WeekTotal(DateOnly Start, DateOnly End, TimeSpan Logged, TimeSpan Expected)
{
    public int Days => End.DayNumber - Start.DayNumber + 1;
}

/// <summary>
/// Where a period stands: hours logged, missing so far, still to log, and per week. Pure logic over
/// the period's days and saved entries.
/// </summary>
public sealed class PeriodSummary
{
    public required Period Period { get; init; }
    public required TimeSpan Logged { get; init; }
    /// <summary>Hours short on workdays up to today.</summary>
    public required TimeSpan Missing { get; init; }
    /// <summary>Hours expected on the workdays after today.</summary>
    public required TimeSpan StillToLog { get; init; }
    public required IReadOnlyList<WeekTotal> Weeks { get; init; }

    public static PeriodSummary For(Period period, IReadOnlyDictionary<DateOnly, DayInfo> days, IReadOnlyList<TimeEntry> entries, DateOnly today)
    {
        var logged = entries.Where(e => period.Contains(e.Date)).GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => g.Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration));
        TimeSpan LoggedOn(DateOnly d) => logged.GetValueOrDefault(d);
        TimeSpan ExpectedOn(DateOnly d) => days.TryGetValue(d, out var info) ? info.Expected : TimeSpan.Zero;

        var missing = TimeSpan.Zero;
        var still = TimeSpan.Zero;
        foreach (var d in period.Days)
        {
            if (d <= today) missing += ExpectedOn(d) > LoggedOn(d) ? ExpectedOn(d) - LoggedOn(d) : TimeSpan.Zero;
            else still += ExpectedOn(d);
        }

        var weeks = period.Days
            .GroupBy(d => d.AddDays(-(((int)d.DayOfWeek + 6) % 7))) // Monday of the week
            .Select(g => new WeekTotal(g.First(), g.Last(),
                g.Aggregate(TimeSpan.Zero, (t, d) => t + LoggedOn(d)),
                g.Aggregate(TimeSpan.Zero, (t, d) => t + ExpectedOn(d))))
            .ToList();

        return new PeriodSummary
        {
            Period = period,
            Logged = logged.Values.Aggregate(TimeSpan.Zero, (t, h) => t + h),
            Missing = missing,
            StillToLog = still,
            Weeks = weeks,
        };
    }

    /// <summary>Nothing short up to today, nothing unsaved and everything in Jira.</summary>
    public bool IsComplete(int notInJira, bool hasUnsaved) => Missing == TimeSpan.Zero && notInJira == 0 && !hasUnsaved;

    /// <summary>True in the last <paramref name="days"/> days of the period that contains today.</summary>
    public static bool EndsSoon(Period period, DateOnly today, int days = 3) =>
        period.Contains(today) && period.End.DayNumber - today.DayNumber < days;
}
