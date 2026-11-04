namespace JiraTime.Domain;

/// <summary>
/// The workdays in a row the user logged in full, counted back from today: the header's flame. Pure logic.
/// </summary>
/// <param name="Days">Complete workdays in a row.</param>
/// <param name="Capped">True when the row goes back further than the days that were read, so it is at least <paramref name="Days"/>.</param>
public sealed record Streak(int Days, bool Capped)
{
    /// <summary>The flame shows from this many days in a row.</summary>
    public const int ShownFrom = 2;

    /// <summary>Days in a row at which the flame grows a size.</summary>
    public static readonly int[] Sizes = [5, 10, 20];

    public static readonly Streak None = new(0, false);

    public bool Shows => Days >= ShownFrom;

    /// <summary>0 below 5 days, 1 from 5, 2 from 10, 3 from 20.</summary>
    public int Size => Sizes.Count(days => Days >= days);

    /// <summary>
    /// Counts complete workdays back from <paramref name="today"/>, skipping weekends, holidays and whole days off.
    /// Today counts once it is complete; an incomplete today doesn't break the row, as the day isn't over. A day
    /// before <paramref name="from"/> or not in <paramref name="days"/> wasn't read, so counting stops there.
    /// </summary>
    /// <param name="entries">Saved work (in Jira or waiting for it), at least from <paramref name="from"/> to today.</param>
    public static Streak Count(DateOnly today, DateOnly from, IReadOnlyDictionary<DateOnly, DayInfo> days, IEnumerable<TimeEntry> entries)
    {
        var logged = entries.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => TimeEntry.Total(g));
        var count = 0;
        for (var day = today; day >= from; day = day.AddDays(-1))
        {
            if (!days.TryGetValue(day, out var info)) return new Streak(count, count > 0);
            if (info.Expected <= TimeSpan.Zero) continue;
            var complete = logged.GetValueOrDefault(day) >= info.Expected;
            if (complete) count++;
            else if (day != today) return new Streak(count, false);
        }
        return new Streak(count, count > 0);
    }

    /// <summary>True when the row got longer since <paramref name="before"/> was seen, and the flame shows: time for it to flare.</summary>
    public static bool Grew(Streak? before, Streak now) => before is not null && now.Shows && now.Days > before.Days;
}
