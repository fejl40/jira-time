using JiraTime.Domain;
using static JiraTime.Tests.Support.TestEntries;

namespace JiraTime.Tests.Domain;

public class StreakTests
{
    private static DateOnly Day(int month, int day) => new(2026, month, day);

    /// <summary>Every day from <paramref name="from"/> to <paramref name="to"/>, Danish holidays and <paramref name="off"/> included.</summary>
    private static Dictionary<DateOnly, DayInfo> Days(DateOnly from, DateOnly to, params DayOff[] off)
    {
        var daysOff = off.ToDictionary(o => o.Date);
        var days = new Dictionary<DateOnly, DayInfo>();
        for (var d = from; d <= to; d = d.AddDays(1)) days[d] = DayInfo.For(d, daysOff);
        return days;
    }

    private static List<TimeEntry> FullDays(params DateOnly[] days) => days.Select(d => Saved("ABC-1", 8, d)).ToList();

    private static Streak Count(DateOnly today, IEnumerable<TimeEntry> entries, params DayOff[] off)
    {
        var from = today.AddDays(-60);
        return Streak.Count(today, from, Days(from, today, off), entries);
    }

    [Fact]
    public void Complete_workdays_count_back_over_the_weekend_and_the_period_edge()
    {
        // Wed 21 Oct, nothing logged yet; Tue 20, Mon 19 and Fri 16 are complete, Thu 15 (the period before) is short.
        var entries = FullDays(Day(10, 20), Day(10, 19), Day(10, 16));
        entries.Add(Saved("ABC-1", 6, Day(10, 15)));

        var streak = Count(Day(10, 21), entries);

        Assert.Equal(new Streak(3, false), streak);
    }

    [Fact]
    public void Today_counts_once_it_is_complete()
    {
        Assert.Equal(2, Count(Day(10, 21), FullDays(Day(10, 21), Day(10, 20))).Days);
        Assert.Equal(1, Count(Day(10, 21), [.. FullDays(Day(10, 20)), Saved("ABC-1", 7.5, Day(10, 21))]).Days);
    }

    [Fact]
    public void A_short_day_before_today_ends_the_streak()
    {
        var entries = FullDays(Day(10, 21), Day(10, 19), Day(10, 16));
        entries.Add(Saved("ABC-1", 4, Day(10, 20)));

        Assert.Equal(1, Count(Day(10, 21), entries).Days);
    }

    [Fact]
    public void Holidays_and_weekends_are_skipped()
    {
        // Mon 28 Dec; Christmas Day (Fri 25) and the weekend are skipped back to Thu 24 and Wed 23.
        var streak = Count(Day(12, 28), FullDays(Day(12, 28), Day(12, 24), Day(12, 23)));

        Assert.Equal(3, streak.Days);
    }

    [Fact]
    public void A_whole_day_off_is_skipped_and_part_of_a_day_off_needs_only_the_rest()
    {
        var off = new DayOff { Date = Day(10, 19), Reason = DayOffReason.Vacation };
        var partial = new DayOff { Date = Day(10, 16), Reason = DayOffReason.Sick, Hours = TimeSpan.FromHours(3) };
        var entries = FullDays(Day(10, 21), Day(10, 20));
        entries.Add(Saved("ABC-1", 5, Day(10, 16)));

        Assert.Equal(3, Count(Day(10, 21), entries, off, partial).Days);
    }

    [Fact]
    public void Hours_on_a_weekend_neither_count_nor_break_the_streak()
    {
        var entries = FullDays(Day(10, 19), Day(10, 16));
        entries.Add(Saved("ABC-1", 2, Day(10, 18)));

        Assert.Equal(2, Count(Day(10, 19), entries).Days);
    }

    [Fact]
    public void A_streak_older_than_the_days_read_is_capped()
    {
        var today = Day(10, 21);
        var from = Day(10, 12);
        var entries = Enumerable.Range(0, 10).Select(i => from.AddDays(i)).Select(d => Saved("ABC-1", 8, d)).ToList();

        var streak = Streak.Count(today, from, Days(from, today), entries);

        Assert.Equal(new Streak(8, true), streak); // Mon 12 to Wed 21: eight workdays, and maybe more before
    }

    [Fact]
    public void Nothing_logged_is_no_streak()
    {
        Assert.Equal(new Streak(0, false), Count(Day(10, 21), []));
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(1, false, 0)]
    [InlineData(2, true, 0)]
    [InlineData(4, true, 0)]
    [InlineData(5, true, 1)]
    [InlineData(9, true, 1)]
    [InlineData(10, true, 2)]
    [InlineData(19, true, 2)]
    [InlineData(20, true, 3)]
    [InlineData(45, true, 3)]
    public void The_flame_shows_from_two_days_and_grows_at_five_ten_and_twenty(int days, bool shows, int size)
    {
        var streak = new Streak(days, false);

        Assert.Equal((shows, size), (streak.Shows, streak.Size));
    }

    [Fact]
    public void The_flame_flares_only_when_a_streak_seen_before_gets_longer()
    {
        Assert.True(Streak.Grew(new Streak(2, false), new Streak(3, false)));
        Assert.True(Streak.Grew(new Streak(0, false), new Streak(2, false)));
        Assert.False(Streak.Grew(null, new Streak(5, false)));             // the first count in a tab
        Assert.False(Streak.Grew(new Streak(5, false), new Streak(5, false)));
        Assert.False(Streak.Grew(new Streak(5, false), new Streak(4, false)));
        Assert.False(Streak.Grew(new Streak(0, false), new Streak(1, false))); // no flame yet
    }
}
