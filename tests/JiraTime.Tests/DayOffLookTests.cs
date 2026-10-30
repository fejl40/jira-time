using JiraTime.Components;
using JiraTime.Domain;

namespace JiraTime.Tests;

public class DayOffLookTests
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
    public void A_whole_day_off_says_why(DayOffReason reason, string label) => Assert.Equal(label, DayOffLook.Label(Day(reason)));

    [Fact]
    public void Part_of_a_day_off_says_how_many_hours() => Assert.Equal("Sick · 3.5 h off", DayOffLook.Label(Day(DayOffReason.Sick, 3.5)));

    [Fact]
    public void The_day_list_on_phones_gets_short_labels()
    {
        Assert.Equal("Vacation", DayOffLook.ShortLabel(Day(DayOffReason.Vacation)));
        Assert.Equal("Time off 3 h", DayOffLook.ShortLabel(Day(DayOffReason.Other, 3)));
        Assert.Null(DayOffLook.ShortLabel(DayInfo.For(Tuesday, new Dictionary<DateOnly, DayOff>())));
    }

    [Fact]
    public void A_day_without_time_off_has_no_label_or_look()
    {
        var workday = DayInfo.For(Tuesday, new Dictionary<DateOnly, DayOff>());

        Assert.Null(DayOffLook.Label(workday));
        Assert.Equal("", DayOffLook.Classes(workday));
        Assert.Null(DayOffLook.Data(workday));
        Assert.Null(DayOffLook.Label(null));
    }

    [Fact]
    public void A_whole_day_off_is_striped_in_its_reason_and_part_of_a_day_only_tinted()
    {
        Assert.Equal("day-off-vacation day-off-whole", DayOffLook.Classes(Day(DayOffReason.Vacation)));
        Assert.Equal("day-off-other day-off-partial", DayOffLook.Classes(Day(DayOffReason.Other, 2)));
    }

    [Fact]
    public void Every_reason_has_its_own_icon_and_tint()
    {
        var reasons = Enum.GetValues<DayOffReason>();

        Assert.Equal(reasons.Length, reasons.Select(DayOffLook.Icon).Distinct().Count());
        Assert.Equal(reasons.Length, reasons.Select(DayOffLook.Badge).Distinct().Count());
    }

    [Fact]
    public void Browser_checks_read_the_reason_in_lower_case() => Assert.Equal("sick", DayOffLook.Data(Day(DayOffReason.Sick, 2)));
}
