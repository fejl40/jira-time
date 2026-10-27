namespace JiraTime.Domain;

/// <summary>
/// A semi-monthly timesheet period: the 1st to the 15th, or the 16th to the last day of the month.
/// </summary>
public readonly record struct Period
{
    public const int SplitDay = 15;

    public DateOnly Start { get; }
    public DateOnly End { get; }

    private Period(DateOnly start, DateOnly end) => (Start, End) = (start, end);

    public int Length => End.DayNumber - Start.DayNumber + 1;
    public Period Next => For(End.AddDays(1));
    public Period Previous => For(Start.AddDays(-1));
    public IEnumerable<DateOnly> Days => Enumerable.Range(0, Length).Select(Start.AddDays);
    public bool Contains(DateOnly d) => d >= Start && d <= End;

    /// <summary>The period containing <paramref name="date"/>.</summary>
    public static Period For(DateOnly date)
    {
        if (date.Day <= SplitDay)
            return new Period(new DateOnly(date.Year, date.Month, 1), new DateOnly(date.Year, date.Month, SplitDay));

        var last = DateTime.DaysInMonth(date.Year, date.Month);
        return new Period(new DateOnly(date.Year, date.Month, SplitDay + 1), new DateOnly(date.Year, date.Month, last));
    }
}
