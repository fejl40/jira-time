using JiraTime.Domain;

namespace JiraTime.Tests;

public class PeriodSummaryTests
{
    // 16-31 Oct 2026: Fri 16, weeks Mon 19 and Mon 26 start inside it.
    private static readonly Period Second = Period.For(new DateOnly(2026, 10, 16));
    private static readonly DateOnly Today = new(2026, 10, 21); // Wednesday

    private static IReadOnlyDictionary<DateOnly, DayInfo> Days(Period p) =>
        p.Days.ToDictionary(d => d, d => DayInfo.For(d, new Dictionary<DateOnly, DayOff>()));

    private static TimeEntry E(string date, double hours) =>
        new() { IssueKey = "ABC-1", Date = DateOnly.Parse(date), Duration = TimeSpan.FromHours(hours) };

    [Fact]
    public void Weeks_are_split_on_mondays_and_cut_to_the_period()
    {
        var summary = PeriodSummary.For(Second, Days(Second), [E("2026-10-16", 8), E("2026-10-19", 6), E("2026-10-20", 8)], Today);

        Assert.Equal(3, summary.Weeks.Count);
        Assert.Equal((new DateOnly(2026, 10, 16), new DateOnly(2026, 10, 18)), (summary.Weeks[0].Start, summary.Weeks[0].End));
        Assert.Equal(TimeSpan.FromHours(8), summary.Weeks[0].Logged);
        Assert.Equal(TimeSpan.FromHours(8), summary.Weeks[0].Expected);
        Assert.Equal(TimeSpan.FromHours(14), summary.Weeks[1].Logged);
        Assert.Equal(TimeSpan.FromHours(40), summary.Weeks[1].Expected);
        Assert.Equal((new DateOnly(2026, 10, 26), new DateOnly(2026, 10, 31), 6), (summary.Weeks[2].Start, summary.Weeks[2].End, summary.Weeks[2].Days));
    }

    [Fact]
    public void Missing_counts_up_to_today_and_the_rest_is_still_to_log()
    {
        var summary = PeriodSummary.For(Second, Days(Second), [E("2026-10-16", 8), E("2026-10-19", 6), E("2026-10-20", 9)], Today);

        Assert.Equal(TimeSpan.FromHours(23), summary.Logged);
        Assert.Equal(TimeSpan.FromHours(2 + 8), summary.Missing);       // Mon short 2, Wed 8; Tue's extra hour doesn't count
        Assert.Equal(TimeSpan.FromHours(7 * 8), summary.StillToLog);    // Thu 22 to Fri 30
    }

    [Theory]
    [InlineData(0, false, true)]
    [InlineData(1, false, false)]
    [InlineData(0, true, false)]
    public void Complete_needs_no_missing_hours_nothing_unsaved_and_everything_in_Jira(int notInJira, bool unsaved, bool complete)
    {
        var entries = new[] { "2026-10-16", "2026-10-19", "2026-10-20", "2026-10-21" }.Select(d => E(d, 8)).ToList();

        var summary = PeriodSummary.For(Second, Days(Second), entries, Today);

        Assert.Equal(complete, summary.IsComplete(notInJira, unsaved));
    }

    [Fact]
    public void A_short_day_means_not_complete() =>
        Assert.False(PeriodSummary.For(Second, Days(Second), [], Today).IsComplete(0, false));

    [Theory]
    [InlineData("2026-10-28", false)]
    [InlineData("2026-10-29", true)]
    [InlineData("2026-10-31", true)]
    [InlineData("2026-11-01", false)]
    public void Ends_soon_in_the_last_three_days(string today, bool soon) =>
        Assert.Equal(soon, PeriodSummary.EndsSoon(Second, DateOnly.Parse(today)));
}
