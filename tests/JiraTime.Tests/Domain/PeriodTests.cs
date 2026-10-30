using JiraTime.Domain;

namespace JiraTime.Tests;

public class PeriodTests
{
    private static DateOnly D(string s) => DateOnly.Parse(s);

    [Theory]
    [InlineData("2026-10-01", "2026-10-01", "2026-10-15")]
    [InlineData("2026-10-15", "2026-10-01", "2026-10-15")]
    [InlineData("2026-10-16", "2026-10-16", "2026-10-31")]
    [InlineData("2026-10-31", "2026-10-16", "2026-10-31")]
    [InlineData("2026-09-30", "2026-09-16", "2026-09-30")] // 30-day month
    [InlineData("2026-02-20", "2026-02-16", "2026-02-28")] // February
    [InlineData("2028-02-29", "2028-02-16", "2028-02-29")] // leap year
    public void For_returns_the_half_month_containing_the_date(string date, string start, string end)
    {
        var p = Period.For(D(date));

        Assert.Equal(D(start), p.Start);
        Assert.Equal(D(end), p.End);
        Assert.True(p.Contains(D(date)));
    }

    [Theory]
    [InlineData("2026-10-01", 15)]
    [InlineData("2026-10-16", 16)]
    [InlineData("2026-09-16", 15)]
    [InlineData("2026-02-16", 13)]
    [InlineData("2028-02-16", 14)]
    public void Length_and_days_match_the_period(string date, int length)
    {
        var p = Period.For(D(date));

        Assert.Equal(length, p.Length);
        Assert.Equal(length, p.Days.Count());
        Assert.Equal(p.Start, p.Days.First());
        Assert.Equal(p.End, p.Days.Last());
    }

    [Theory]
    [InlineData("2026-10-05", "2026-10-16", "2026-09-16")]
    [InlineData("2026-10-20", "2026-11-01", "2026-10-01")]
    [InlineData("2026-12-20", "2027-01-01", "2026-12-01")] // across year end
    [InlineData("2027-01-10", "2027-01-16", "2026-12-16")]
    public void Next_and_previous_step_between_half_months(string date, string nextStart, string previousStart)
    {
        var p = Period.For(D(date));

        Assert.Equal(D(nextStart), p.Next.Start);
        Assert.Equal(D(previousStart), p.Previous.Start);
        Assert.Equal(p, p.Next.Previous);
        Assert.Equal(p, p.Previous.Next);
    }

    [Fact]
    public void A_period_does_not_contain_the_days_around_it()
    {
        var p = Period.For(D("2026-10-20"));

        Assert.False(p.Contains(D("2026-10-15")));
        Assert.False(p.Contains(D("2026-11-01")));
    }
}
