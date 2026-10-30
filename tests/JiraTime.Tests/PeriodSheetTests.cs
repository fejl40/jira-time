using JiraTime.Domain;

namespace JiraTime.Tests;

/// <summary>A period being worked on: missing hours, suggestions, unsaved days and totals, grid rows and days off.</summary>
public class PeriodSheetTests
{
    // Fri 16 Oct .. Sat 31 Oct 2026; today is Tue 20 Oct.
    private static readonly Period Period = Period.For(new DateOnly(2026, 10, 20));
    private static readonly DateOnly Friday = new(2026, 10, 16), Monday = new(2026, 10, 19), Tuesday = new(2026, 10, 20), Wednesday = new(2026, 10, 21);
    private static readonly IssueWithStatus Doing = new("ABC-1", "Login page", "Story", "In Progress", "indeterminate");

    private static TimeEntry Saved(string key, double hours, DateOnly date, string? worklog = "1") => new()
    {
        IssueKey = key, IssueSummary = key + " summary", Date = date, Duration = TimeSpan.FromHours(hours),
        JiraWorklogId = worklog, SyncPending = worklog is null,
    };

    private static Dictionary<DateOnly, DayInfo> Days(params DayOff[] off) =>
        Period.Days.ToDictionary(d => d, d => DayInfo.For(d, off.ToDictionary(o => o.Date)));

    private static PeriodSheet Sheet(params TimeEntry[] entries)
    {
        var sheet = new PeriodSheet(Period, () => Tuesday) { Sprint = [Doing] };
        sheet.Load(entries, [], Days());
        return sheet;
    }

    private static string Show(IEnumerable<TimeEntry> rows) => string.Join(", ", rows.Select(r => $"{r.IssueKey} {r.Duration.TotalHours}h"));

    /// <summary>The day's new rows (suggested or added).</summary>
    private static string Drafts(PeriodSheet sheet, DateOnly day) => Show(sheet.Day(day).Rows.Where(r => r.IsDraft).Select(r => r.Entry));

    private static TimeEntry Add(PeriodSheet sheet, DateOnly day, string key, double hours)
    {
        var row = sheet.Day(day).AddRow(new JiraIssue(key, ""));
        sheet.Day(day).SetHours(row, hours.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return row;
    }

    [Fact]
    public void Missing_hours_count_up_to_today_only()
    {
        var sheet = Sheet(Saved("XYZ-1", 8, Friday), Saved("XYZ-2", 2, Monday));

        Assert.Equal(TimeSpan.Zero, sheet.Day(Friday).Missing);
        Assert.Equal(TimeSpan.FromHours(6), sheet.Day(Monday).Missing);
        Assert.Equal(TimeSpan.FromHours(8), sheet.Day(Tuesday).Missing);
        Assert.Equal(TimeSpan.Zero, sheet.Day(Wednesday).Missing);
        Assert.False(sheet.CanFill(Wednesday));
    }

    [Fact]
    public void Fill_period_suggests_for_short_workdays_up_to_today()
    {
        var sheet = Sheet(Saved("XYZ-1", 8, Friday), Saved("XYZ-2", 2, Monday));

        sheet.FillPeriod();

        Assert.Equal([Monday, Tuesday], sheet.UnsavedDays);
        Assert.Equal("XYZ-2 6h", Drafts(sheet, Monday));  // partly logged: the rest on its issue
        Assert.Equal("XYZ-2 8h", Drafts(sheet, Tuesday)); // copies Monday as it will be
        Assert.Empty(sheet.OwnChangeDays); // untouched suggestions are not the user's changes
    }

    [Fact]
    public void A_finished_day_is_not_suggested_again_until_fill_period_or_restart()
    {
        var sheet = Sheet();
        sheet.Suggest(Monday);
        sheet.Finish(Monday);

        sheet.Suggest(Monday);
        Assert.False(sheet.IsUnsaved(Monday));

        sheet.Restart(Monday);
        Assert.True(sheet.IsUnsaved(Monday));
    }

    [Fact]
    public void Rows_list_each_issue_once_with_unsaved_issues_and_the_empty_issue_last()
    {
        var sheet = Sheet(Saved("XYZ-2", 4, Friday), Saved("abc-9", 4, Friday));
        Add(sheet, Monday, "DEF-1", 1);
        Add(sheet, Monday, "", 1);

        Assert.Equal(["abc-9", "DEF-1", "XYZ-2", ""], sheet.Rows.Select(r => r.Key));
        Assert.Empty(sheet.Rows.Single(r => r.Key == "DEF-1").Entries); // only saved entries count as the row's entries
    }

    [Fact]
    public void Unsaved_totals_cover_every_changed_day()
    {
        var keep = Saved("XYZ-1", 4, Friday);
        var gone = Saved("XYZ-2", 4, Friday);
        var sheet = Sheet(keep, gone);
        sheet.Day(Friday).Remove(gone);
        Add(sheet, Monday, "ABC-1", 8);

        Assert.Equal(TimeSpan.FromHours(12), sheet.UnsavedTotal);
        Assert.Equal(TimeSpan.Zero, sheet.UnsavedFor(Friday, "XYZ-2"));
        Assert.Null(sheet.UnsavedFor(Friday, "XYZ-1")); // unchanged
        Assert.Equal(TimeSpan.FromHours(8), sheet.UnsavedFor(Monday, "ABC-1"));
        Assert.Null(sheet.UnsavedFor(Tuesday, "ABC-1")); // no changes
    }

    [Fact]
    public void Every_day_object_reads_the_same_unsaved_changes_and_reading_leaves_nothing_behind()
    {
        var sheet = Sheet();
        var shown = sheet.Day(Wednesday);
        Assert.Empty(sheet.Rows);

        Add(sheet, Wednesday, "ABC-1", 2);

        Assert.Equal(TimeSpan.FromHours(2), shown.Total);
        Assert.Equal([Wednesday], sheet.UnsavedDays);
    }

    [Fact]
    public void A_grid_cell_opens_the_issue_row_of_the_day_or_adds_one()
    {
        var sheet = Sheet(Saved("XYZ-1", 8, Friday));

        var existing = sheet.RowFor(Friday, "xyz-1", "");
        var added = sheet.RowFor(Friday, "DEF-1", "Other");

        Assert.Equal("XYZ-1", existing.IssueKey);
        Assert.Equal(("DEF-1", TimeSpan.Zero), (added.IssueKey, added.Duration));
        Assert.Same(added, sheet.RowFor(Friday, "DEF-1", "Other"));
    }

    [Fact]
    public void The_previous_worked_day_comes_from_the_earlier_entries_before_the_period()
    {
        var sheet = new PeriodSheet(Period, () => Tuesday);
        var thursday = new DateOnly(2026, 10, 15);
        sheet.Load([], [Saved("OLD-1", 8, thursday)], new Dictionary<DateOnly, DayInfo>());

        var previous = sheet.PreviousWorkedDay(Monday);

        Assert.Equal(thursday, previous?.Date);
        Assert.Equal("OLD-1 8h", Show(previous!.Value.Rows));
    }

    [Fact]
    public void Progress_is_the_share_of_the_day_saved()
    {
        var sheet = Sheet(Saved("XYZ-1", 8, Friday), Saved("XYZ-2", 2, Monday), Saved("XYZ-3", 9, Tuesday));
        var saturday = sheet.Day(new DateOnly(2026, 10, 17));
        Add(sheet, Monday, "XYZ-4", 6); // unsaved hours don't count

        Assert.Equal((1d, true), (sheet.Day(Friday).Progress, sheet.Day(Friday).IsComplete));
        Assert.Equal((0.25, false), (sheet.Day(Monday).Progress, sheet.Day(Monday).IsComplete));
        Assert.Equal(1, sheet.Day(Tuesday).Progress); // more than needed is still full
        Assert.Equal((0d, false), (saturday.Progress, saturday.IsComplete));
    }

    [Fact]
    public void Without_Jira_hours_are_unknown_so_nothing_is_missing_or_suggested()
    {
        var sheet = new PeriodSheet(Period, () => Tuesday) { Sprint = [Doing] };
        sheet.Load([], [], Days(), hoursKnown: false);

        Assert.Equal(TimeSpan.Zero, sheet.Day(Monday).Missing);
        Assert.False(sheet.CanFill(Monday));
        sheet.FillPeriod();
        Assert.Empty(sheet.UnsavedDays);
    }

    [Fact]
    public void A_grid_row_gives_its_entries_on_one_day()
    {
        var monday = new TimeEntry { IssueKey = "ABC-1", Date = Monday, Duration = TimeSpan.FromHours(2) };
        var tuesday = new TimeEntry { IssueKey = "ABC-1", Date = Monday.AddDays(1), Duration = TimeSpan.FromHours(3) };
        var row = new PeriodRow("ABC-1", "", [monday, tuesday]);

        Assert.Equal([monday], row.On(Monday));
        Assert.Empty(row.On(Monday.AddDays(2)));
    }

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
