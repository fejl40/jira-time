using System.Globalization;

namespace JiraTime.Domain;

/// <summary>
/// One week of a period as a ring that closes as its logged hours reach what the week needs (within the period).
/// <see cref="Share"/> is null when Jira couldn't be read, so the hours are unknown.
/// </summary>
public record WeekRing(DateOnly Start, int Number, TimeSpan Logged, TimeSpan Needed, double? Share)
{
    /// <summary>The week has all the hours it needs.</summary>
    public bool IsClosed => Share >= 1;

    /// <summary>The rings of a period: one per week that needs hours, in order. A week that needs none (a weekend at
    /// the period's edge, a week off) gets no ring.</summary>
    public static IReadOnlyList<WeekRing> For(PeriodSummary summary)
    {
        var rings = new List<WeekRing>();
        foreach (var week in summary.Weeks)
        {
            if (week.Expected <= TimeSpan.Zero) continue;
            var share = summary.JiraHoursKnown ? Math.Min(1, week.Logged / week.Expected) : (double?)null;
            rings.Add(new WeekRing(week.Start, ISOWeek.GetWeekOfYear(week.Start.ToDateTime(TimeOnly.MinValue)), week.Logged, week.Expected, share));
        }
        return rings;
    }

    /// <summary>The week starts of the closed rings, for noticing a ring that closes while the user watches.</summary>
    public static IEnumerable<DateOnly> Closed(IEnumerable<WeekRing> rings) => rings.Where(r => r.IsClosed).Select(r => r.Start);
}
