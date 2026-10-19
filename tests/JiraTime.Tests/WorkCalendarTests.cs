using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage.FileSystem;

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

        Assert.Equal(TimeSpan.FromHours(missing), WorkCalendarService.Missing(day, TimeSpan.FromHours(logged)));
    }

    [Fact]
    public async Task Days_off_can_be_marked_and_unmarked()
    {
        using var dir = new TempDir();
        using var store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = dir.Path });
        var calendar = new WorkCalendarService(store);
        var day = D("2026-10-20");

        await calendar.MarkDayOffAsync(day, DayOffReason.Vacation);
        var marked = await calendar.GetDayAsync(day);
        Assert.Equal(DayKind.DayOff, marked.Kind);
        Assert.Equal("Vacation", marked.Name);
        Assert.Equal(TimeSpan.Zero, marked.Expected);

        await calendar.UnmarkDayOffAsync(day);
        Assert.Equal(DayKind.Workday, (await calendar.GetDayAsync(day)).Kind);
    }

    [Fact]
    public void Default_workday_is_8_hours_excluding_lunch()
    {
        var w = Workday.Default;

        Assert.Null(w.Validate());
        Assert.Equal(Workday.Target, (w.End - w.Start) - (w.LunchEnd - w.LunchStart));
    }

    [Theory]
    [InlineData("16:00", "08:00", "12:00", "12:30")]
    [InlineData("08:00", "16:30", "12:30", "12:00")]
    [InlineData("08:00", "16:30", "07:00", "07:30")]
    public void Invalid_workdays_are_rejected(string start, string end, string lunchStart, string lunchEnd) =>
        Assert.NotNull(new Workday(TimeOnly.Parse(start), TimeOnly.Parse(end), TimeOnly.Parse(lunchStart), TimeOnly.Parse(lunchEnd)).Validate());
}
