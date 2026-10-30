using JiraTime.Components;
using JiraTime.Domain;

namespace JiraTime.Tests.Components;

public class DayLookTests
{
    private static readonly DateOnly Tuesday = new(2026, 10, 20);

    private static DayInfo Day(DayOffReason reason, double? hoursOff = null) =>
        DayInfo.For(Tuesday, new Dictionary<DateOnly, DayOff>
        {
            [Tuesday] = new() { Date = Tuesday, Reason = reason, Hours = hoursOff is { } h ? TimeSpan.FromHours(h) : null },
        });

    [Theory]
    [InlineData(DayOffReason.Vacation, "Vacation · whole day")]
    [InlineData(DayOffReason.Sick, "Sick · whole day")]
    [InlineData(DayOffReason.Other, "Time off · whole day")]
    public void A_whole_day_off_says_why(DayOffReason reason, string label) => Assert.Equal(label, DayLook.Label(Day(reason)));

    [Fact]
    public void Part_of_a_day_off_says_how_many_hours() => Assert.Equal("Sick · 3.5 h off", DayLook.Label(Day(DayOffReason.Sick, 3.5)));

    [Fact]
    public void The_day_list_on_phones_gets_short_labels()
    {
        Assert.Equal("Vacation", DayLook.ShortLabel(Day(DayOffReason.Vacation)));
        Assert.Equal("Time off 3 h", DayLook.ShortLabel(Day(DayOffReason.Other, 3)));
        Assert.Null(DayLook.ShortLabel(DayInfo.For(Tuesday, new Dictionary<DateOnly, DayOff>())));
    }

    [Fact]
    public void A_day_without_time_off_has_no_label_or_look()
    {
        var workday = DayInfo.For(Tuesday, new Dictionary<DateOnly, DayOff>());

        Assert.Null(DayLook.Label(workday));
        Assert.Equal("", DayLook.TimeOffClasses(workday));
        Assert.Null(DayLook.Data(workday));
        Assert.Null(DayLook.Label(null));
    }

    [Fact]
    public void A_whole_day_off_is_striped_in_its_reason_and_part_of_a_day_only_tinted()
    {
        Assert.Equal("day-off-vacation day-off-whole", DayLook.TimeOffClasses(Day(DayOffReason.Vacation)));
        Assert.Equal("day-off-other day-off-partial", DayLook.TimeOffClasses(Day(DayOffReason.Other, 2)));
    }

    [Fact]
    public void Every_reason_has_its_own_icon_and_tint()
    {
        var reasons = Enum.GetValues<DayOffReason>();

        Assert.Equal(reasons.Length, reasons.Select(DayLook.Icon).Distinct().Count());
        Assert.Equal(reasons.Length, reasons.Select(DayLook.Badge).Distinct().Count());
    }

    [Fact]
    public void Browser_checks_read_the_reason_in_lower_case() => Assert.Equal("sick", DayLook.Data(Day(DayOffReason.Sick, 2)));

    private static DayInfo Plain(string date) => DayInfo.For(DateOnly.Parse(date), new Dictionary<DateOnly, DayOff>());

    [Fact]
    public void Weekends_and_holidays_are_grey_and_other_days_are_not()
    {
        Assert.Equal("bg-slate-50 dark:bg-slate-800/60", DayLook.Background(Plain("2026-10-24")));   // Saturday
        Assert.Equal("bg-slate-50 dark:bg-slate-800/60", DayLook.Background(Plain("2026-12-25")));   // Christmas
        Assert.Equal("", DayLook.Background(Plain("2026-10-20")));
        Assert.Equal("", DayLook.Background(Day(DayOffReason.Vacation)));
        Assert.Equal("", DayLook.Background(null));
    }

    [Fact]
    public void Weekends_and_holidays_are_named_and_other_days_are_not()
    {
        Assert.Equal("Weekend", DayLook.KindName(Plain("2026-10-24")));
        Assert.Equal("Christmas Day", DayLook.KindName(Plain("2026-12-25")));
        Assert.Equal("No work hours", DayLook.KindName(new DayInfo(Tuesday, DayKind.Weekend, "No work hours")));
        Assert.Null(DayLook.KindName(Plain("2026-10-20")));
        Assert.Null(DayLook.KindName(Day(DayOffReason.Vacation)));
        Assert.Null(DayLook.KindName(null));
    }

    [Fact]
    public void The_column_title_names_the_holiday_or_the_time_off()
    {
        Assert.Equal("Christmas Day", DayLook.Title(Plain("2026-12-25")));
        Assert.Equal("Sick · 2 h off", DayLook.Title(Day(DayOffReason.Sick, 2)));
        Assert.Equal("Vacation · whole day", DayLook.Title(Day(DayOffReason.Vacation)));
        Assert.Null(DayLook.Title(Plain("2026-10-24")));
        Assert.Null(DayLook.Title(Plain("2026-10-20")));
    }
}
