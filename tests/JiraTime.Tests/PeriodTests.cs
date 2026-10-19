using JiraTime.Domain;

namespace JiraTime.Tests;

public class PeriodTests
{
    private static readonly DateOnly Anchor = new(2026, 1, 5); // a Monday

    [Theory]
    [InlineData("2026-01-05", "2026-01-05")] // the anchor itself
    [InlineData("2026-01-18", "2026-01-05")] // last day of the first period
    [InlineData("2026-01-19", "2026-01-19")] // first day of the next one
    [InlineData("2026-10-19", "2026-10-12")]
    [InlineData("2026-01-04", "2025-12-22")] // before the anchor
    [InlineData("2025-12-22", "2025-12-22")]
    public void For_returns_the_period_containing_the_date(string date, string expectedStart)
    {
        var period = Period.For(DateOnly.Parse(date), Anchor);

        Assert.Equal(DateOnly.Parse(expectedStart), period.Start);
        Assert.True(period.Contains(DateOnly.Parse(date)));
    }

    [Fact]
    public void Period_spans_14_days()
    {
        var p = new Period(Anchor);

        Assert.Equal(new DateOnly(2026, 1, 18), p.End);
        Assert.Equal(14, p.Days.Count());
        Assert.Equal(p.Start, p.Days.First());
        Assert.Equal(p.End, p.Days.Last());
    }

    [Fact]
    public void Next_and_previous_step_14_days()
    {
        var p = new Period(Anchor);

        Assert.Equal(new DateOnly(2026, 1, 19), p.Next.Start);
        Assert.Equal(new DateOnly(2025, 12, 22), p.Previous.Start);
        Assert.Equal(p, p.Next.Previous);
    }

    [Fact]
    public void Contains_excludes_days_outside()
    {
        var p = new Period(Anchor);

        Assert.False(p.Contains(p.Start.AddDays(-1)));
        Assert.False(p.Contains(p.End.AddDays(1)));
    }
}
