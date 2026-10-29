using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage;

namespace JiraTime.Tests;

public class WorkCalendarTests
{
    private static DateOnly D(string s) => DateOnly.Parse(s);

    [Theory]
    [InlineData(2024, "2024-03-31")]
    [InlineData(2025, "2025-04-20")]
    [InlineData(2026, "2026-04-05")]
    [InlineData(2027, "2027-03-28")]
    [InlineData(2038, "2038-04-25")]
    public void Easter_sunday(int year, string expected) =>
        Assert.Equal(D(expected), DanishHolidays.EasterSunday(year));

    [Fact]
    public void Danish_holidays_2026()
    {
        var days = DanishHolidays.For(2026).Keys.Order().ToList();

        Assert.Equal(
            new[] { "2026-01-01", "2026-04-02", "2026-04-03", "2026-04-05", "2026-04-06", "2026-05-14", "2026-05-24", "2026-05-25", "2026-12-25", "2026-12-26" }.Select(D),
            days);
    }

    [Fact]
    public void Great_prayer_day_only_before_2024()
    {
        Assert.Equal("Great Prayer Day", DanishHolidays.NameOf(D("2023-05-05")));
        Assert.DoesNotContain("Great Prayer Day", DanishHolidays.For(2024).Values);
    }

    [Theory]
    [InlineData("2026-10-19", DayKind.Workday)]  // Monday
    [InlineData("2026-10-24", DayKind.Weekend)]  // Saturday
    [InlineData("2026-04-03", DayKind.Holiday)]  // Good Friday
    [InlineData("2026-12-25", DayKind.Holiday)]  // Christmas, a Friday
    public void Classify(string date, DayKind expected) =>
        Assert.Equal(expected, DanishHolidays.Classify(D(date), new Dictionary<DateOnly, DayOff>()).Kind);

    [Fact]
    public void Expected_hours_are_8_on_workdays_only()
    {
        Assert.Equal(TimeSpan.FromHours(8), new DayInfo(D("2026-10-19"), DayKind.Workday, null).Expected);
        Assert.Equal(TimeSpan.Zero, new DayInfo(D("2026-10-24"), DayKind.Weekend, null).Expected);
        Assert.Equal(TimeSpan.Zero, new DayInfo(D("2026-04-03"), DayKind.Holiday, "Good Friday").Expected);
        Assert.Equal(TimeSpan.Zero, new DayInfo(D("2026-10-20"), DayKind.DayOff, "Vacation").Expected);
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(5.5, 2.5)]
    [InlineData(8, 0)]
    [InlineData(9, 0)]
    public void Missing_is_target_minus_logged_never_negative(double logged, double missing)
    {
        var day = new DayInfo(D("2026-10-19"), DayKind.Workday, null);

        Assert.Equal(TimeSpan.FromHours(missing), day.MissingWith(TimeSpan.FromHours(logged)));
    }

    [Fact]
    public async Task Days_off_can_be_marked_and_unmarked()
    {
        using var dir = new TempDir();
        using var store = new FileDocumentStore(dir.Path);
        var calendar = new WorkCalendarService(store, TestUsers.SignedIn());
        var day = D("2026-10-20");

        await calendar.MarkDayOffAsync(day, DayOffReason.Vacation);
        var marked = await calendar.GetDayAsync(day);
        Assert.Equal(DayKind.DayOff, marked.Kind);
        Assert.Equal("Vacation", marked.Name);
        Assert.Equal(TimeSpan.Zero, marked.Expected);

        await calendar.UnmarkDayOffAsync(day);
        Assert.Equal(DayKind.Workday, (await calendar.GetDayAsync(day)).Kind);
    }

    private static (TempDir, FileDocumentStore, WorkCalendarService) Calendar()
    {
        var dir = new TempDir();
        var store = new FileDocumentStore(dir.Path);
        return (dir, store, new WorkCalendarService(store, TestUsers.SignedIn()));
    }

    [Fact]
    public async Task A_partial_day_off_lowers_the_expected_hours()
    {
        var (dir, store, calendar) = Calendar();
        using var _ = dir; using var __ = store;
        var day = D("2026-10-20");

        await calendar.MarkDayOffAsync(day, DayOffReason.Other, TimeSpan.FromHours(3));
        var partial = await calendar.GetDayAsync(day);
        Assert.Equal(DayKind.Workday, partial.Kind);
        Assert.True(partial.HasPartialDayOff);
        Assert.Equal("Other", partial.Name);
        Assert.Equal(TimeSpan.FromHours(5), partial.Expected);
        Assert.Equal(TimeSpan.FromHours(1), partial.MissingWith(TimeSpan.FromHours(4)));
        Assert.Equal(TimeSpan.FromHours(5), DayFiller.Propose(partial, [], []).Single().Duration);

        await calendar.UnmarkDayOffAsync(day);
        Assert.Equal(TimeSpan.FromHours(8), (await calendar.GetDayAsync(day)).Expected);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    public async Task Time_off_of_the_whole_day_or_more_is_a_day_off(double hours)
    {
        var (dir, store, calendar) = Calendar();
        using var _ = dir; using var __ = store;
        var day = D("2026-10-20");

        await calendar.MarkDayOffAsync(day, DayOffReason.Sick, TimeSpan.FromHours(hours));

        var info = await calendar.GetDayAsync(day);
        Assert.Equal(DayKind.DayOff, info.Kind);
        Assert.Equal(TimeSpan.Zero, info.Expected);
    }

    [Fact]
    public async Task Work_hours_per_weekday_are_saved_per_user_and_used_for_every_day()
    {
        var (dir, store, calendar) = Calendar();
        using var _ = dir; using var __ = store;
        var week = UserSettings.StandardWeek();
        week[DayOfWeek.Friday] = TimeSpan.FromHours(5);
        week[DayOfWeek.Monday] = TimeSpan.FromHours(7.5);
        week[DayOfWeek.Saturday] = TimeSpan.FromHours(2);
        week[DayOfWeek.Wednesday] = TimeSpan.Zero;

        Assert.Null(await calendar.SaveWorkHoursAsync(week));
        var days = await calendar.GetDaysAsync(D("2026-10-19"), D("2026-10-25"));

        Assert.Equal(TimeSpan.FromHours(7.5), days[D("2026-10-19")].Expected); // Monday
        Assert.Equal(DayKind.Weekend, days[D("2026-10-21")].Kind);             // Wednesday, no hours
        Assert.Equal(TimeSpan.FromHours(5), days[D("2026-10-23")].Expected);   // Friday
        Assert.Equal(DayKind.Workday, days[D("2026-10-24")].Kind);             // Saturday with hours
        Assert.Equal(TimeSpan.FromHours(2), days[D("2026-10-24")].Expected);
        Assert.Equal(TimeSpan.Zero, days[D("2026-10-25")].Expected);           // Sunday

        var anna = new WorkCalendarService(store, TestUsers.SignedIn(TestUsers.Anna));
        Assert.Equal(TimeSpan.FromHours(8), (await anna.GetDayAsync(D("2026-10-23"))).Expected);
    }

    [Fact]
    public async Task Partial_time_off_is_taken_from_the_users_hours_for_that_day()
    {
        var (dir, store, calendar) = Calendar();
        using var _ = dir; using var __ = store;
        var week = UserSettings.StandardWeek();
        week[DayOfWeek.Friday] = TimeSpan.FromHours(6);
        await calendar.SaveWorkHoursAsync(week);

        await calendar.MarkDayOffAsync(D("2026-10-23"), DayOffReason.Other, TimeSpan.FromHours(2));

        Assert.Equal(TimeSpan.FromHours(4), (await calendar.GetDayAsync(D("2026-10-23"))).Expected);
    }

    [Fact]
    public async Task Holidays_need_no_hours_whatever_the_work_hours()
    {
        var (dir, store, calendar) = Calendar();
        using var _ = dir; using var __ = store;

        var goodFriday = await calendar.GetDayAsync(D("2026-04-03"));

        Assert.Equal(DayKind.Holiday, goodFriday.Kind);
        Assert.Equal(TimeSpan.Zero, goodFriday.Expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(7.501)]
    public async Task Invalid_work_hours_are_not_saved(double hours)
    {
        var (dir, store, calendar) = Calendar();
        using var _ = dir; using var __ = store;
        var week = UserSettings.StandardWeek();
        week[DayOfWeek.Monday] = TimeSpan.FromHours(hours);

        Assert.NotNull(await calendar.SaveWorkHoursAsync(week));
        Assert.Equal(TimeSpan.FromHours(8), (await calendar.GetDayAsync(D("2026-10-19"))).Expected);
    }
}
