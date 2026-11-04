using JiraTime.Domain;
using JiraTime.Tests.Support;
using static JiraTime.Tests.Support.TestEntries;

namespace JiraTime.Tests.Domain;

public class WeekRingsTests
{
    // 16-31 Oct 2026: Fri 16 (week 42), Mon 19 (week 43), Mon 26 (week 44).
    private static readonly Period Second = Period.For(new DateOnly(2026, 10, 16));
    // 1-15 Nov 2026: Sun 1 (week 44, a weekend only), Mon 2 (week 45), Mon 9 (week 46).
    private static readonly Period FirstOfNovember = Period.For(new DateOnly(2026, 11, 1));
    private static readonly DateOnly Today = new(2026, 10, 21);

    private static DateOnly October(int day) => new(2026, 10, day);

    private static IReadOnlyList<WeekRing> Rings(Period period, IReadOnlyList<TimeEntry> entries, bool hoursKnown = true, params DayOff[] off) =>
        WeekRing.For(PeriodSummary.For(period, TestSheets.Days(period, off), entries, Today, hoursKnown));

    [Fact]
    public void Each_week_gets_its_share_of_the_hours_it_needs_within_the_period()
    {
        var rings = Rings(Second, [Saved("ABC-1", 4, October(16)), Saved("ABC-1", 8, October(19)), Saved("ABC-1", 2, October(20))]);

        Assert.Equal([42, 43, 44], rings.Select(r => r.Number));
        Assert.Equal(0.5, rings[0].Share);                     // part week: Friday only, 4 of 8 h
        Assert.Equal(TimeSpan.FromHours(8), rings[0].Needed);
        Assert.Equal(10.0 / 40, rings[1].Share);
        Assert.Equal(0.0, rings[2].Share);
        Assert.Equal(TimeSpan.FromHours(40), rings[2].Needed); // Mon 26 to Fri 30; Sat 31 needs nothing
    }

    [Fact]
    public void A_ring_is_full_at_the_hours_the_week_needs_and_never_more()
    {
        var rings = Rings(Second, [Saved("ABC-1", 6, October(16)), Saved("ABC-1", 6, October(16), worklog: "2")]);

        Assert.Equal(1.0, rings[0].Share);
        Assert.True(rings[0].IsClosed);
        Assert.Equal(TimeSpan.FromHours(12), rings[0].Logged);
        Assert.False(rings[1].IsClosed);
    }

    [Fact]
    public void A_week_that_needs_no_hours_gets_no_ring()
    {
        var rings = Rings(FirstOfNovember, []);

        Assert.Equal([45, 46], rings.Select(r => r.Number)); // Sunday 1 November alone needs nothing
        Assert.Equal(new DateOnly(2026, 11, 2), rings[0].Start);
    }

    [Fact]
    public void Days_off_lower_what_the_week_needs()
    {
        var off = new[]
        {
            new DayOff { Date = October(19) },                                  // whole day
            new DayOff { Date = October(20), Hours = TimeSpan.FromHours(4) },   // half a day
        };

        var rings = Rings(Second, [Saved("ABC-1", 28, October(21))], true, off);

        Assert.Equal(TimeSpan.FromHours(28), rings[1].Needed);
        Assert.True(rings[1].IsClosed);
    }

    [Fact]
    public void A_week_off_entirely_gets_no_ring()
    {
        var off = Enumerable.Range(26, 5).Select(d => new DayOff { Date = October(d) }).ToArray();

        var rings = Rings(Second, [], true, off);

        Assert.Equal([42, 43], rings.Select(r => r.Number));
    }

    [Fact]
    public void Shares_are_unknown_when_Jira_could_not_be_read()
    {
        var rings = Rings(Second, [Waiting("ABC-1", 8, October(16))], hoursKnown: false);

        Assert.All(rings, r => Assert.Null(r.Share));
        Assert.All(rings, r => Assert.False(r.IsClosed));
        Assert.Equal(TimeSpan.FromHours(8), rings[0].Needed);
    }

    [Fact]
    public void Closed_gives_the_starts_of_the_full_weeks()
    {
        var rings = Rings(Second, [Saved("ABC-1", 8, October(16))]);

        Assert.Equal([October(16)], WeekRing.Closed(rings));
    }
}
