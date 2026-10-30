using JiraTime.Components;
using JiraTime.Components.Sheet;
using JiraTime.Domain;

namespace JiraTime.Tests;

/// <summary>A user whose browser is in another time zone than the server: today and worklog start times are on the user's clock.</summary>
public class UserTimeZoneTests
{
    private static readonly TimeZoneInfo NewYork = Zone(-5), Copenhagen = Zone(1), Tokyo = Zone(9);

    private static TimeZoneInfo Zone(int hours) =>
        TimeZoneInfo.CreateCustomTimeZone($"UTC{hours:+0;-0}", TimeSpan.FromHours(hours), $"UTC{hours:+0;-0}", $"UTC{hours:+0;-0}");

    private static TimeHarness SignedInFrom(TimeZoneInfo? browser) => new(TimeHarness.Server with { TimeZone = browser });

    private static ShownPeriod NewShownPeriod(TimeHarness h)
    {
        var (loader, statuses) = h.PeriodLoading();
        return new ShownPeriod(loader, statuses, h.Sync, h.Log, h.Calendar, h.User, h.Clock);
    }

    [Theory]
    [InlineData("2026-10-16T02:00:00Z", -5, "2026-10-15")] // the server is past midnight, the user isn't yet
    [InlineData("2026-10-15T20:00:00Z", 9, "2026-10-16")]  // the user is past midnight, the server isn't yet
    [InlineData("2026-10-15T20:00:00Z", 0, "2026-10-15")]
    public void Today_is_the_day_in_the_given_zone(string utcNow, int hours, string today)
    {
        var clock = new TestClock(DateTimeOffset.Parse(utcNow)) { Zone = Copenhagen };

        Assert.Equal(DateOnly.Parse(today), clock.Today(Zone(hours)));
    }

    [Fact]
    public void Without_a_zone_today_is_the_servers_day()
    {
        var clock = new TestClock(DateTimeOffset.Parse("2026-10-15T23:30:00Z")) { Zone = Copenhagen };

        Assert.Equal(new DateOnly(2026, 10, 16), clock.Today());
    }

    [Fact]
    public void Near_midnight_the_timesheet_opens_the_users_day_and_period()
    {
        using var h = SignedInFrom(NewYork);
        h.Clock.Zone = Copenhagen;
        h.Clock.Now = DateTimeOffset.Parse("2026-10-16T02:00:00Z"); // 03:00 on the 16th in Copenhagen, 21:00 on the 15th in New York

        var today = NewShownPeriod(h).Today;

        Assert.Equal(new DateOnly(2026, 10, 15), today);
        Assert.Equal(new DateOnly(2026, 10, 1), Period.For(today).Start);
    }

    [Fact]
    public void A_user_whose_browser_gave_no_zone_gets_the_servers_day()
    {
        using var h = SignedInFrom(null);
        h.Clock.Zone = Tokyo;
        h.Clock.Now = DateTimeOffset.Parse("2026-10-15T20:00:00Z");

        Assert.Null(h.User.TimeZone);
        Assert.Equal(new DateOnly(2026, 10, 16), NewShownPeriod(h).Today);
    }

    [Fact]
    public async Task Recent_work_is_read_up_to_the_users_today()
    {
        using var h = SignedInFrom(Tokyo);
        h.Clock.Now = DateTimeOffset.Parse("2026-10-15T20:00:00Z"); // the 16th in Tokyo, the 15th on the UTC server

        await h.Log.GetRecentAsync();

        Assert.Contains(h.Jira.WorklogSearches, r => FakeJira.Jql(r).Contains("worklogDate <= \"2026-10-16\""));
    }

    [Theory]
    [InlineData(-5, "2026-10-19T09:00:00.000-0500")]
    [InlineData(9, "2026-10-19T09:00:00.000+0900")]
    public async Task Worklogs_start_at_9_oclock_on_the_users_clock(int hours, string started)
    {
        using var h = SignedInFrom(Zone(hours));
        h.Clock.Zone = Copenhagen;

        await h.SaveDayAsync(new DateOnly(2026, 10, 19), [TestEntries.Row("ABC-1", 1, null)]);

        Assert.Equal(started, (string?)Assert.Single(h.Jira.Writes).Body!["started"]);
    }

    [Fact]
    public async Task Without_a_browser_zone_worklogs_start_on_the_servers_clock()
    {
        using var h = SignedInFrom(null);
        h.Clock.Zone = Copenhagen;

        await h.SaveDayAsync(new DateOnly(2026, 10, 19), [TestEntries.Row("ABC-1", 1, null)]);

        Assert.Equal("2026-10-19T09:00:00.000+0100", (string?)Assert.Single(h.Jira.Writes).Body!["started"]);
    }
}
