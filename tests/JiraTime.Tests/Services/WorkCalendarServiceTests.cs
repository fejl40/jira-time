using JiraTime.Domain;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services;

/// <summary>Days off and work hours saved and read through <see cref="JiraTime.Services.WorkCalendarService"/>.</summary>
public sealed class WorkCalendarServiceTests : IDisposable
{
    private readonly TimeHarness _harness = new();

    private JiraTime.Services.WorkCalendarService Calendar => _harness.Calendar;

    private static DateOnly D(string s) => DateOnly.Parse(s);

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task Days_off_can_be_marked_and_unmarked()
    {
        var day = D("2026-10-20");

        await Calendar.MarkDayOffAsync(day, DayOffReason.Vacation);
        var marked = await Calendar.GetDayAsync(day);
        Assert.Equal(DayKind.DayOff, marked.Kind);
        Assert.Equal("Vacation", marked.Name);
        Assert.Equal(DayOffReason.Vacation, marked.Reason);
        Assert.Equal(TimeSpan.Zero, marked.Expected);

        await Calendar.UnmarkDayOffAsync(day);
        Assert.Equal(DayKind.Workday, (await Calendar.GetDayAsync(day)).Kind);
        Assert.Null((await Calendar.GetDayAsync(day)).Reason);
    }

    [Fact]
    public async Task A_partial_day_off_lowers_the_expected_hours()
    {
        var day = D("2026-10-20");

        await Calendar.MarkDayOffAsync(day, DayOffReason.Other, TimeSpan.FromHours(3));
        var partial = await Calendar.GetDayAsync(day);
        Assert.Equal(DayKind.Workday, partial.Kind);
        Assert.True(partial.HasPartialDayOff);
        Assert.Equal("Other", partial.Name);
        Assert.Equal(DayOffReason.Other, partial.Reason);
        Assert.Equal(TimeSpan.FromHours(5), partial.Expected);
        Assert.Equal(TimeSpan.FromHours(1), partial.MissingWith(TimeSpan.FromHours(4)));
        Assert.Equal(TimeSpan.FromHours(5), DayFiller.Propose(partial, [], []).Single().Duration);

        await Calendar.UnmarkDayOffAsync(day);
        Assert.Equal(TimeSpan.FromHours(8), (await Calendar.GetDayAsync(day)).Expected);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    public async Task Time_off_of_the_whole_day_or_more_is_a_day_off(double hours)
    {
        var day = D("2026-10-20");

        await Calendar.MarkDayOffAsync(day, DayOffReason.Sick, TimeSpan.FromHours(hours));

        var info = await Calendar.GetDayAsync(day);
        Assert.Equal(DayKind.DayOff, info.Kind);
        Assert.Equal(DayOffReason.Sick, info.Reason);
        Assert.Equal(TimeSpan.Zero, info.Expected);
    }

    [Fact]
    public async Task Work_hours_per_weekday_are_saved_per_user_and_used_for_every_day()
    {
        var week = UserSettings.StandardWeek();
        week[DayOfWeek.Friday] = TimeSpan.FromHours(5);
        week[DayOfWeek.Monday] = TimeSpan.FromHours(7.5);
        week[DayOfWeek.Saturday] = TimeSpan.FromHours(2);
        week[DayOfWeek.Wednesday] = TimeSpan.Zero;

        Assert.Null(await Calendar.SaveWorkHoursAsync(week));
        var days = await Calendar.GetDaysAsync(D("2026-10-19"), D("2026-10-25"));

        Assert.Equal(TimeSpan.FromHours(7.5), days[D("2026-10-19")].Expected); // Monday
        Assert.Equal(DayKind.Weekend, days[D("2026-10-21")].Kind);             // Wednesday, no hours
        Assert.Equal(TimeSpan.FromHours(5), days[D("2026-10-23")].Expected);   // Friday
        Assert.Equal(DayKind.Workday, days[D("2026-10-24")].Kind);             // Saturday with hours
        Assert.Equal(TimeSpan.FromHours(2), days[D("2026-10-24")].Expected);
        Assert.Equal(TimeSpan.Zero, days[D("2026-10-25")].Expected);           // Sunday

        var anna = _harness.For(TestUsers.Anna).Calendar;
        Assert.Equal(TimeSpan.FromHours(8), (await anna.GetDayAsync(D("2026-10-23"))).Expected);
    }

    [Fact]
    public async Task Partial_time_off_is_taken_from_the_users_hours_for_that_day()
    {
        var week = UserSettings.StandardWeek();
        week[DayOfWeek.Friday] = TimeSpan.FromHours(6);
        await Calendar.SaveWorkHoursAsync(week);

        await Calendar.MarkDayOffAsync(D("2026-10-23"), DayOffReason.Other, TimeSpan.FromHours(2));

        Assert.Equal(TimeSpan.FromHours(4), (await Calendar.GetDayAsync(D("2026-10-23"))).Expected);
    }

    [Fact]
    public async Task Holidays_need_no_hours_whatever_the_work_hours()
    {

        var goodFriday = await Calendar.GetDayAsync(D("2026-04-03"));

        Assert.Equal(DayKind.Holiday, goodFriday.Kind);
        Assert.Equal(TimeSpan.Zero, goodFriday.Expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(7.501)]
    public async Task Invalid_work_hours_are_not_saved(double hours)
    {
        var week = UserSettings.StandardWeek();
        week[DayOfWeek.Monday] = TimeSpan.FromHours(hours);

        Assert.NotNull(await Calendar.SaveWorkHoursAsync(week));
        Assert.Equal(TimeSpan.FromHours(8), (await Calendar.GetDayAsync(D("2026-10-19"))).Expected);
    }
}
