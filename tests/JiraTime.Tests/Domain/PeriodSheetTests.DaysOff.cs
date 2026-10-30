using JiraTime.Domain;
using static JiraTime.Tests.TestEntries;

namespace JiraTime.Tests;

/// <summary>Days off on a period being worked on. Part of <see cref="PeriodSheetTests"/>, split out to keep each file under 250 lines.</summary>
public partial class PeriodSheetTests
{
    private static Func<Task> Reload(PeriodSheet sheet, TimeEntry[] entries, params DayOff[] off) => () =>
    {
        sheet.Load(entries, [], Days(off));
        return Task.CompletedTask;
    };

    private static DayOff HoursOff(double hours) => new() { Date = Monday, Hours = TimeSpan.FromHours(hours) };

    [Fact]
    public async Task Part_of_a_day_off_remakes_an_untouched_suggestion_for_the_new_hours()
    {
        TimeEntry[] saved = [Saved("XYZ-1", 2, Monday)];
        var sheet = Sheet(saved);
        sheet.Suggest(Monday);
        Assert.Equal("XYZ-1 6h", Drafts(sheet, Monday));
        var reload = Reload(sheet, saved, HoursOff(4));

        await sheet.MarkDayOffAsync(Monday, wholeDay: false, async () =>
        {
            Assert.Equal("", Drafts(sheet, Monday)); // the old suggestion doesn't show meanwhile
            await reload();
        });

        Assert.Equal("XYZ-1 2h", Drafts(sheet, Monday));
    }

    [Fact]
    public async Task Part_of_a_day_off_keeps_the_users_own_rows()
    {
        TimeEntry[] saved = [Saved("XYZ-1", 2, Monday)];
        var sheet = Sheet(saved);
        Add(sheet, Monday, "ABC-1", 6);

        await sheet.MarkDayOffAsync(Monday, wholeDay: false, Reload(sheet, saved, HoursOff(4)));

        Assert.Equal("ABC-1 6h", Drafts(sheet, Monday));
    }

    [Fact]
    public async Task A_whole_day_off_marks_saved_work_removed_drops_suggestions_and_keeps_added_rows()
    {
        TimeEntry[] saved = [Saved("XYZ-1", 2, Monday), Saved("XYZ-2", 1, Monday)];
        var sheet = Sheet(saved);
        sheet.Suggest(Monday);
        Add(sheet, Monday, "XYZ-3", 1);

        await sheet.MarkDayOffAsync(Monday, wholeDay: true, Reload(sheet, saved, new DayOff { Date = Monday }));

        Assert.Equal([RowState.Removed, RowState.Removed, RowState.New], sheet.Day(Monday).Rows.Select(r => r.State));
        Assert.Contains(Monday, sheet.UnsavedDays); // Save all deletes the worklogs
    }

    [Fact]
    public async Task Undoing_a_day_off_brings_the_work_back_and_suggests_the_day_again()
    {
        TimeEntry[] saved = [Saved("XYZ-1", 2, Monday)];
        var sheet = Sheet(saved);
        sheet.Suggest(Monday);
        await sheet.MarkDayOffAsync(Monday, wholeDay: true, Reload(sheet, saved, new DayOff { Date = Monday }));

        await sheet.UnmarkDayOffAsync(Monday, Reload(sheet, saved));

        Assert.Equal([RowState.InJira, RowState.Suggested], sheet.Day(Monday).Rows.Select(r => r.State));
        Assert.Equal("XYZ-1 6h", Drafts(sheet, Monday));
    }
}
