using JiraTime.Domain;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Domain;

public class SeasonTouchesTests
{
    [Theory]
    [InlineData("2026-12-01")]
    [InlineData("2026-12-14")]
    [InlineData("2026-12-31")]
    public void December_days_get_snow(string day)
    {
        var touches = SeasonTouches.For(DateOnly.Parse(day));

        Assert.True(touches.Snow);
        Assert.False(touches.Lights);
    }

    [Theory]
    [InlineData("2026-01-01", "New Year's Day")]
    [InlineData("2026-04-06", "Easter Monday")]
    [InlineData("2026-05-14", "Ascension Day")]
    [InlineData("2026-05-24", "Whit Sunday")]
    public void Danish_holidays_get_lights(string day, string holiday)
    {
        var touches = SeasonTouches.For(DateOnly.Parse(day));

        Assert.Equal(new SeasonTouches(false, holiday), touches);
        Assert.True(touches.Lights);
    }

    [Fact]
    public void Christmas_gets_snow_and_lights()
    {
        var touches = SeasonTouches.For(new DateOnly(2026, 12, 25));

        Assert.Equal((true, true, "Christmas Day"), (touches.Snow, touches.Lights, touches.Holiday));
    }

    [Theory]
    [InlineData("2026-11-30")]
    [InlineData("2026-12-24")] // Christmas Eve isn't a public holiday
    [InlineData("2026-06-05")] // nor is Constitution Day
    [InlineData("2026-01-02")]
    public void Ordinary_days_get_nothing_or_only_snow(string day)
    {
        var date = DateOnly.Parse(day);

        Assert.Equal(new SeasonTouches(date.Month == 12, null), SeasonTouches.For(date));
    }

    [Fact]
    public void Today_is_read_on_the_users_clock()
    {
        // 23:30 UTC on 30 November is already 1 December in Copenhagen, and 31 December 23:30 UTC is New Year's Day there.
        var clock = new TestClock(new DateTimeOffset(2026, 11, 30, 23, 30, 0, TimeSpan.Zero));
        var copenhagen = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

        Assert.False(SeasonTouches.Today(clock, TimeZoneInfo.Utc).Snow);
        Assert.True(SeasonTouches.Today(clock, copenhagen).Snow);

        clock.Now = new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero);
        Assert.Equal(new SeasonTouches(true, null), SeasonTouches.Today(clock, TimeZoneInfo.Utc));
        Assert.Equal(new SeasonTouches(false, "New Year's Day"), SeasonTouches.Today(clock, copenhagen));
    }
}
