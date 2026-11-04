using JiraTime.Components;
using JiraTime.Domain;

namespace JiraTime.Tests.Components;

/// <summary>How hours, days and counts are written for the user.</summary>
public class FormatTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(90, "1.5")]
    [InlineData(20, "0.3333")]
    [InlineData(480, "8")]
    public void Hours_for_scripts_are_never_empty_and_use_a_dot(int minutes, string expected) =>
        Assert.Equal(expected, Format.HoursValue(TimeSpan.FromMinutes(minutes)));

    [Fact]
    public void Hours_text_and_days_read_well()
    {
        Assert.Equal("0 h", Format.HoursText(TimeSpan.Zero));
        Assert.Equal("7.5 h", Format.HoursText(TimeSpan.FromHours(7.5)));
        Assert.Equal("1 day", Format.Days(1));
        Assert.Equal("3 days", Format.Days(3));
    }

    [Fact]
    public void Days_are_written_in_english()
    {
        var day = new DateOnly(2025, 11, 3);
        Assert.Equal("Monday 3 Nov", Format.Day(day));
        Assert.Equal("Mon 3 Nov", Format.ShortDay(day));
        Assert.Equal("Monday 3 November", Format.LongDay(day));
        Assert.Equal("Mon 3", Format.WeekdayAndNumber(day));
        Assert.Equal("Mo", Format.TwoLetterWeekday(day));
        Assert.Equal("November 2025", Format.Month(day));
    }

    [Theory]
    [InlineData(0, "0%")]
    [InlineData(0.625, "62.5%")]
    [InlineData(1, "100%")]
    public void Shares_become_css_percentages(double share, string expected) => Assert.Equal(expected, Format.Percent(share));

    [Theory]
    [InlineData(1, "row", "1 row")]
    [InlineData(0, "row", "0 rows")]
    [InlineData(3, "row", "3 rows")]
    [InlineData(1, "entry", "1 entry")]
    [InlineData(2, "entry", "2 entries")]
    [InlineData(2, "day", "2 days")]
    public void Counts_are_plural_unless_one(int n, string word, string expected) => Assert.Equal(expected, Format.Count(n, word));

    [Theory]
    [InlineData(85, "85 ms")]
    [InlineData(1420, "1.4 s")]
    public void Request_durations_read_in_ms_or_seconds(long ms, string expected) => Assert.Equal(expected, Format.Duration(ms));

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(2_411_724, "2.3 MB")]
    public void Sizes_read_in_bytes_KB_or_MB(long bytes, string expected) => Assert.Equal(expected, Format.Bytes(bytes));

    private static readonly DateOnly Monday = new(2026, 10, 26);

    [Theory]
    [InlineData(32, 40, "Week 44: 32 of 40 h")]
    [InlineData(0, 40, "Week 44: 0 of 40 h")]
    [InlineData(7.5, 8, "Week 44: 7.5 of 8 h")]
    public void A_week_ring_says_its_hours_against_what_the_week_needs(double logged, double needed, string text) =>
        Assert.Equal(text, Format.WeekRing(new WeekRing(Monday, 44, TimeSpan.FromHours(logged), TimeSpan.FromHours(needed), logged / needed)));

    [Fact]
    public void A_week_ring_says_when_its_hours_are_unknown() =>
        Assert.Equal("Week 44: hours unknown, Jira couldn't be read (40 h needed)",
            Format.WeekRing(new WeekRing(Monday, 44, TimeSpan.Zero, TimeSpan.FromHours(40), null)));
}
