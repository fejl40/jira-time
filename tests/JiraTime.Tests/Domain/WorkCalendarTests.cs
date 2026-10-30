using JiraTime.Domain;

namespace JiraTime.Tests.Domain;

public class WorkCalendarTests
{
    private static DateOnly D(string s) => DateOnly.Parse(s);

    [Theory]
    [InlineData(2024, "2024-03-31")]
    [InlineData(2025, "2025-04-20")]
    [InlineData(2026, "2026-04-05")]
    [InlineData(2027, "2027-03-28")]
    [InlineData(2038, "2038-04-25")]
    public void Easter_sunday_is_worked_out_for_any_year(int year, string expected) =>
        Assert.Equal(D(expected), DanishHolidays.EasterSunday(year));

    [Fact]
    public void The_Danish_holidays_of_2026_are_all_known()
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
    public void A_day_is_a_workday_a_weekend_or_a_holiday(string date, DayKind expected) =>
        Assert.Equal(expected, DayInfo.For(D(date), new Dictionary<DateOnly, DayOff>()).Kind);

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

    [Theory]
    [InlineData("7:30", 7.5)]
    [InlineData("7,5", 7.5)]
    [InlineData("0", 0)]
    [InlineData("", 0)]
    public void Typed_work_hours_are_read(string monday, double hours)
    {
        var week = WorkWeekInput.Parse(Typed(monday));

        Assert.Null(week.Error);
        Assert.Equal(TimeSpan.FromHours(hours), week.Hours![DayOfWeek.Monday]);
    }

    [Fact]
    public void The_week_total_adds_up_every_day()
    {
        var typed = Typed("7:30");
        typed[DayOfWeek.Saturday] = "2";

        Assert.Equal(TimeSpan.FromHours(41.5), WorkWeekInput.Parse(typed).Total);
    }

    [Fact]
    public void Hours_that_cant_be_read_name_the_first_such_day_and_leave_no_total()
    {
        var typed = Typed("8");
        typed[DayOfWeek.Thursday] = "lots";
        typed[DayOfWeek.Friday] = "x";

        var week = WorkWeekInput.Parse(typed);

        Assert.Null(week.Hours);
        Assert.Null(week.Total);
        Assert.Equal("Thursday: enter hours like 7.5 or 7:30.", week.Error);
    }

    private static Dictionary<DayOfWeek, string> Typed(string monday) => new()
    {
        [DayOfWeek.Monday] = monday, [DayOfWeek.Tuesday] = "8", [DayOfWeek.Wednesday] = "8", [DayOfWeek.Thursday] = "8",
        [DayOfWeek.Friday] = "8", [DayOfWeek.Saturday] = "0", [DayOfWeek.Sunday] = "",
    };
}
