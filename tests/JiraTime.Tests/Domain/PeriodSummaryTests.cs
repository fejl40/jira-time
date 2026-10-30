using JiraTime.Domain;
using static JiraTime.Tests.TestEntries;

namespace JiraTime.Tests;

public class PeriodSummaryTests
{
    // 16-31 Oct 2026: Fri 16, weeks Mon 19 and Mon 26 start inside it.
    private static readonly Period Second = Period.For(new DateOnly(2026, 10, 16));
    private static readonly DateOnly Today = new(2026, 10, 21); // Wednesday

    private static DateOnly October(int day) => new(2026, 10, day);

    [Fact]
    public void Weeks_are_split_on_mondays_and_cut_to_the_period()
    {
        var summary = PeriodSummary.For(Second, TestSheets.Days(Second), [Saved("ABC-1", 8, October(16)), Saved("ABC-1", 6, October(19)), Saved("ABC-1", 8, October(20))], Today);

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
        var summary = PeriodSummary.For(Second, TestSheets.Days(Second), [Saved("ABC-1", 8, October(16)), Saved("ABC-1", 6, October(19)), Saved("ABC-1", 9, October(20))], Today);

        Assert.Equal(TimeSpan.FromHours(23), summary.Logged);
        Assert.Equal(TimeSpan.FromHours(2 + 8), summary.Missing);       // Mon short 2, Wed 8; Tue's extra hour doesn't count
        Assert.Equal(TimeSpan.FromHours(7 * 8), summary.StillToLog);    // Thu 22 to Fri 30
    }

    private static List<TimeEntry> FullDaysUpToToday() =>
        new[] { 16, 19, 20, 21 }.Select(day => Saved("ABC-1", 8, October(day))).ToList();

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void Complete_needs_no_missing_hours_nothing_unsaved_and_everything_in_Jira(bool waitingForJira, bool unsaved, bool complete)
    {
        var entries = FullDaysUpToToday();
        if (waitingForJira) entries[0].Failed("Jira was down");

        var summary = PeriodSummary.For(Second, TestSheets.Days(Second), entries, Today);

        Assert.Equal(complete, summary.IsComplete(unsaved));
    }

    [Fact]
    public void A_short_day_means_not_complete() =>
        Assert.False(PeriodSummary.For(Second, TestSheets.Days(Second), [], Today).IsComplete(false));

    [Fact]
    public void Work_not_in_Jira_is_counted_in_the_period_only()
    {
        var waiting = new[] { Saved("ABC-1", 1, October(19)), Saved("ABC-1", 2, October(20)), Saved("ABC-1", 3, October(15)) };
        foreach (var e in waiting) e.Failed("Jira was down");

        var summary = PeriodSummary.For(Second, TestSheets.Days(Second), [.. waiting, Saved("ABC-1", 4, October(21))], Today);

        Assert.Equal(2, summary.NotInJira);
    }

    [Fact]
    public void Missing_and_logged_are_given_per_day()
    {
        var summary = PeriodSummary.For(Second, TestSheets.Days(Second), [Saved("ABC-1", 6, October(19)), Saved("ABC-1", 1, October(19))], Today);

        Assert.Equal(TimeSpan.FromHours(7), summary.LoggedOn(new DateOnly(2026, 10, 19)));
        Assert.Equal(TimeSpan.FromHours(1), summary.MissingOn(new DateOnly(2026, 10, 19)));
        Assert.Equal(TimeSpan.FromHours(8), summary.MissingOn(new DateOnly(2026, 10, 20)));
        Assert.Equal(TimeSpan.Zero, summary.MissingOn(new DateOnly(2026, 10, 22))); // after today
        Assert.Equal(TimeSpan.Zero, summary.MissingOn(new DateOnly(2026, 10, 17))); // Saturday
    }

    [Fact]
    public void Unknown_hours_mean_nothing_is_missing_and_the_period_is_not_complete()
    {
        var waiting = Saved("ABC-1", 2, October(19));
        waiting.Failed("Jira was down");

        var summary = PeriodSummary.For(Second, TestSheets.Days(Second), [waiting], Today, hoursKnown: false);

        Assert.False(summary.JiraHoursKnown);
        Assert.Equal(TimeSpan.Zero, summary.Missing);
        Assert.Equal(TimeSpan.Zero, summary.MissingOn(new DateOnly(2026, 10, 20)));
        Assert.Equal(1, summary.NotInJira);
        Assert.False(summary.IsComplete(false));
    }

    [Fact]
    public void Unknown_hours_with_full_days_are_still_not_complete() =>
        Assert.False(PeriodSummary.For(Second, TestSheets.Days(Second), FullDaysUpToToday(), Today, hoursKnown: false).IsComplete(false));

    [Fact]
    public void Without_Jira_the_Jira_hours_are_unknown_and_the_calendar_hours_keep_their_values()
    {
        var waiting = Saved("ABC-1", 2, October(19));
        waiting.Failed("Jira was down");
        List<TimeEntry> entries = [Saved("ABC-1", 8, October(16)), waiting];
        var known = PeriodSummary.For(Second, TestSheets.Days(Second), entries, Today);

        var unknown = PeriodSummary.For(Second, TestSheets.Days(Second), [waiting], Today, hoursKnown: false);

        Assert.Equal(TimeSpan.FromHours(2), unknown.Logged); // only the work waiting for Jira
        Assert.Equal(TimeSpan.Zero, unknown.Missing);
        Assert.False(unknown.IsComplete(false));
        Assert.Equal(known.StillToLog, unknown.StillToLog);
        Assert.Equal(TimeSpan.FromHours(7 * 8), unknown.StillToLog);
        Assert.Equal(known.Weeks.Select(w => w.Expected), unknown.Weeks.Select(w => w.Expected));
        Assert.Equal(TimeSpan.FromHours(8), unknown.ExpectedOn(October(20)));
    }

    [Fact]
    public void Without_Jira_the_end_of_the_period_still_knows_the_hours_for_the_days_left()
    {
        var thursday = October(29);

        var summary = PeriodSummary.For(Second, TestSheets.Days(Second), [], thursday, hoursKnown: false);

        Assert.True(PeriodSummary.EndsSoon(Second, thursday));
        Assert.Equal(TimeSpan.Zero, summary.Missing);
        Assert.Equal(TimeSpan.FromHours(8), summary.StillToLog); // Friday 30; Saturday 31 isn't a workday
    }

    [Theory]
    [InlineData("2026-10-28", false)]
    [InlineData("2026-10-29", true)]
    [InlineData("2026-10-31", true)]
    [InlineData("2026-11-01", false)]
    public void Ends_soon_in_the_last_three_days(string today, bool soon) =>
        Assert.Equal(soon, PeriodSummary.EndsSoon(Second, DateOnly.Parse(today)));
}
