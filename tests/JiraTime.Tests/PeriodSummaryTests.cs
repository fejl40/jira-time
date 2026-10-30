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
        new() { IssueKey = "ABC-1", Date = DateOnly.Parse(date), Duration = TimeSpan.FromHours(hours), JiraWorklogId = "1", SyncPending = false };

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

    private static List<TimeEntry> FullDaysUpToToday() =>
        new[] { "2026-10-16", "2026-10-19", "2026-10-20", "2026-10-21" }.Select(d => E(d, 8)).ToList();

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void Complete_needs_no_missing_hours_nothing_unsaved_and_everything_in_Jira(bool waitingForJira, bool unsaved, bool complete)
    {
        var entries = FullDaysUpToToday();
        if (waitingForJira) entries[0].Failed("Jira was down");

        var summary = PeriodSummary.For(Second, Days(Second), entries, Today);

        Assert.Equal(complete, summary.IsComplete(unsaved));
    }

    [Fact]
    public void A_short_day_means_not_complete() =>
        Assert.False(PeriodSummary.For(Second, Days(Second), [], Today).IsComplete(false));

    [Fact]
    public void Work_not_in_Jira_is_counted_in_the_period_only()
    {
        var waiting = new[] { E("2026-10-19", 1), E("2026-10-20", 2), E("2026-10-15", 3) };
        foreach (var e in waiting) e.Failed("Jira was down");

        var summary = PeriodSummary.For(Second, Days(Second), [.. waiting, E("2026-10-21", 4)], Today);

        Assert.Equal(2, summary.NotInJira);
    }

    [Fact]
    public void Missing_and_logged_are_given_per_day()
    {
        var summary = PeriodSummary.For(Second, Days(Second), [E("2026-10-19", 6), E("2026-10-19", 1)], Today);

        Assert.Equal(TimeSpan.FromHours(7), summary.LoggedOn(new DateOnly(2026, 10, 19)));
        Assert.Equal(TimeSpan.FromHours(1), summary.MissingOn(new DateOnly(2026, 10, 19)));
        Assert.Equal(TimeSpan.FromHours(8), summary.MissingOn(new DateOnly(2026, 10, 20)));
        Assert.Equal(TimeSpan.Zero, summary.MissingOn(new DateOnly(2026, 10, 22))); // after today
        Assert.Equal(TimeSpan.Zero, summary.MissingOn(new DateOnly(2026, 10, 17))); // Saturday
    }

    [Fact]
    public void Unknown_hours_mean_nothing_is_missing_and_the_period_is_not_complete()
    {
        var waiting = E("2026-10-19", 2);
        waiting.Failed("Jira was down");

        var summary = PeriodSummary.For(Second, Days(Second), [waiting], Today, hoursKnown: false);

        Assert.False(summary.HoursKnown);
        Assert.Equal(TimeSpan.Zero, summary.Missing);
        Assert.Equal(TimeSpan.Zero, summary.MissingOn(new DateOnly(2026, 10, 20)));
        Assert.Equal(1, summary.NotInJira);
        Assert.False(summary.IsComplete(false));
    }

    [Fact]
    public void Unknown_hours_with_full_days_are_still_not_complete() =>
        Assert.False(PeriodSummary.For(Second, Days(Second), FullDaysUpToToday(), Today, hoursKnown: false).IsComplete(false));

    [Theory]
    [InlineData("2026-10-28", false)]
    [InlineData("2026-10-29", true)]
    [InlineData("2026-10-31", true)]
    [InlineData("2026-11-01", false)]
    public void Ends_soon_in_the_last_three_days(string today, bool soon) =>
        Assert.Equal(soon, PeriodSummary.EndsSoon(Second, DateOnly.Parse(today)));
}
