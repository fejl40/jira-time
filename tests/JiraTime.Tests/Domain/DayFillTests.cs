using JiraTime.Domain;
using JiraTime.Tests.Support;
using static JiraTime.Tests.Support.TestEntries;

namespace JiraTime.Tests.Domain;

public class DayFillTests
{
    private static readonly DateOnly Saturday = Monday.AddDays(5);

    private static DayFill? FillOn(DateOnly day, TimeEntry[] saved, params DayOff[] off)
    {
        var sheet = new PeriodSheet(Period.For(Monday), () => Monday);
        sheet.Load(saved, [], TestSheets.Days(sheet.Period, off));
        return DayFill.Of(sheet, day);
    }

    [Fact]
    public void A_day_fills_with_its_saved_hours_against_the_hours_it_needs()
    {
        var fill = FillOn(Monday, [Saved("ABC-1", 2), Saved("ABC-2", 4)]);

        Assert.Equal(new DayFill(0.75, DayFillState.Filling), fill);
        Assert.Equal("filling", fill?.Data);
    }

    [Fact]
    public void An_empty_workday_has_water_at_the_bottom()
    {
        Assert.Equal(new DayFill(0, DayFillState.Filling), FillOn(Monday, []));
    }

    [Fact]
    public void A_day_with_all_its_hours_is_full()
    {
        Assert.Equal(new DayFill(1, DayFillState.Full), FillOn(Monday, [Saved("ABC-1", 8)]));
    }

    [Fact]
    public void A_day_over_its_hours_stays_at_the_brim()
    {
        var fill = FillOn(Monday, [Saved("ABC-1", 10)]);

        Assert.Equal(new DayFill(1, DayFillState.Over), fill);
        Assert.Equal("over", fill?.Data);
    }

    [Fact]
    public void Part_of_a_day_off_fills_against_the_hours_left()
    {
        var off = new DayOff { Date = Monday, Reason = DayOffReason.Vacation, Hours = TimeSpan.FromHours(4) };

        Assert.Equal(new DayFill(0.5, DayFillState.Filling), FillOn(Monday, [Saved("ABC-1", 2)], off));
        Assert.Equal(new DayFill(1, DayFillState.Full), FillOn(Monday, [Saved("ABC-1", 4)], off));
    }

    [Fact]
    public void Days_that_need_no_hours_have_no_water()
    {
        var wholeDayOff = new DayOff { Date = Monday, Reason = DayOffReason.Sick };

        Assert.Null(FillOn(Monday, [Saved("ABC-1", 3)], wholeDayOff));
        Assert.Null(FillOn(Saturday, [Saved("ABC-1", 3, Saturday)]));
        Assert.Null(DayFill.Of(TimeSpan.FromHours(1), TimeSpan.Zero));
    }

    [Fact]
    public void No_day_has_water_while_Jira_cannot_be_read()
    {
        var sheet = new PeriodSheet(Period.For(Monday), () => Monday);
        sheet.Load([], [], TestSheets.Days(sheet.Period), hoursKnown: false);

        Assert.Null(DayFill.Of(sheet, Monday));
    }

    private static PeriodSheet SheetWith(params TimeEntry[] saved) => TestSheets.For(Monday, saved);

    private static void Reload(PeriodSheet sheet, params TimeEntry[] saved) =>
        sheet.Load(saved, [], TestSheets.Days(sheet.Period));

    [Fact]
    public void Levels_at_the_first_look_make_no_wave()
    {
        var water = new DayFills();

        water.Show(SheetWith(Saved("ABC-1", 4)));

        Assert.Equal(0.5, water.Fill(Monday)?.Level);
        Assert.Equal(0, water.Wave(Monday));
    }

    [Fact]
    public void Each_change_of_a_level_makes_one_more_wave_on_that_day_only()
    {
        var tuesday = Monday.AddDays(1);
        var sheet = SheetWith(Saved("ABC-1", 4));
        var water = new DayFills();
        water.Show(sheet);

        Reload(sheet, Saved("ABC-1", 6));
        water.Show(sheet);
        water.Show(sheet);
        Assert.Equal((1, 0), (water.Wave(Monday), water.Wave(tuesday)));

        Reload(sheet, Saved("ABC-1", 8));
        water.Show(sheet);
        Assert.Equal(2, water.Wave(Monday));
        Assert.Equal(DayFillState.Full, water.Fill(Monday)?.State);
    }

    [Fact]
    public void Another_period_starts_without_waves()
    {
        var water = new DayFills();
        var sheet = SheetWith();
        water.Show(sheet);
        Reload(sheet, Saved("ABC-1", 2));
        water.Show(sheet);

        water.Show(SheetWith(Saved("ABC-1", 6)));

        Assert.Equal(0, water.Wave(Monday));
        Assert.Equal(0.75, water.Fill(Monday)?.Level);
    }
}
