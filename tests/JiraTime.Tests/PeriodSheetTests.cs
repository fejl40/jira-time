using JiraTime.Domain;

namespace JiraTime.Tests;

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

    private static PeriodSheet Sheet(params TimeEntry[] entries)
    {
        var sheet = new PeriodSheet(Period, () => Tuesday) { Sprint = [Doing] };
        var days = Period.Days.ToDictionary(d => d, d => DayInfo.For(d, new Dictionary<DateOnly, DayOff>()));
        sheet.Load(entries, [], days);
        return sheet;
    }

    private static string Show(IEnumerable<TimeEntry> rows) => string.Join(", ", rows.Select(r => $"{r.IssueKey} {r.Duration.TotalHours}h"));

    [Fact]
    public void Missing_hours_count_up_to_today_only()
    {
        var sheet = Sheet(Saved("XYZ-1", 8, Friday), Saved("XYZ-2", 2, Monday));

        Assert.Equal(TimeSpan.Zero, sheet.Missing(Friday));
        Assert.Equal(TimeSpan.FromHours(6), sheet.Missing(Monday));
        Assert.Equal(TimeSpan.FromHours(8), sheet.Missing(Tuesday));
        Assert.Equal(TimeSpan.Zero, sheet.Missing(Wednesday));
        Assert.False(sheet.CanFill(Wednesday));
    }

    [Fact]
    public void Fill_period_suggests_for_short_workdays_up_to_today()
    {
        var sheet = Sheet(Saved("XYZ-1", 8, Friday), Saved("XYZ-2", 2, Monday));

        sheet.FillPeriod();

        Assert.Equal([Monday, Tuesday], sheet.UnsavedDays);
        Assert.Equal("XYZ-2 6h", Show(sheet.WorkFor(Monday).Drafts));  // partly logged: the rest on its issue
        Assert.Equal("XYZ-2 8h", Show(sheet.WorkFor(Tuesday).Drafts)); // copies Monday as it will be
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
        sheet.WorkFor(Monday).Drafts.Add(new TimeEntry { IssueKey = "DEF-1", Date = Monday, Duration = TimeSpan.FromHours(1) });
        sheet.WorkFor(Monday).Drafts.Add(new TimeEntry { IssueKey = "", Date = Monday, Duration = TimeSpan.FromHours(1) });

        Assert.Equal(["abc-9", "DEF-1", "XYZ-2", ""], sheet.Rows.Select(r => r.Key));
        Assert.Empty(sheet.Rows.Single(r => r.Key == "DEF-1").Entries); // only saved entries count as the row's entries
    }

    [Fact]
    public void Unsaved_totals_and_removals_cover_every_changed_day()
    {
        var keep = Saved("XYZ-1", 4, Friday);
        var gone = Saved("XYZ-2", 4, Friday);
        var sheet = Sheet(keep, gone);
        sheet.WorkFor(Friday).Removed.Add(gone.Id);
        sheet.WorkFor(Monday).Drafts.Add(new TimeEntry { IssueKey = "ABC-1", Date = Monday, Duration = TimeSpan.FromHours(8) });

        Assert.Equal(TimeSpan.FromHours(12), sheet.UnsavedTotal);
        Assert.Equal(1, sheet.UnsavedRemovals);
        Assert.Equal(TimeSpan.Zero, sheet.UnsavedFor(Friday, "XYZ-2"));
        Assert.Null(sheet.UnsavedFor(Friday, "XYZ-1")); // unchanged
        Assert.Equal(TimeSpan.FromHours(8), sheet.UnsavedFor(Monday, "ABC-1"));
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
    public void Only_suggestions_means_no_edit_removal_or_own_row()
    {
        var saved = Saved("XYZ-1", 2, Monday);
        var sheet = Sheet(saved);
        Assert.True(sheet.HasOnlySuggestions(Monday));

        sheet.Suggest(Monday);
        Assert.True(sheet.HasOnlySuggestions(Monday));

        sheet.WorkFor(Monday).Edit(saved).Duration = TimeSpan.FromHours(3);
        Assert.False(sheet.HasOnlySuggestions(Monday));
    }

    [Fact]
    public void Rows_before_the_period_come_from_the_earlier_entries()
    {
        var sheet = new PeriodSheet(Period, () => Tuesday);
        var thursday = new DateOnly(2026, 10, 15);
        sheet.Load([], [Saved("OLD-1", 8, thursday)], new Dictionary<DateOnly, DayInfo>());

        Assert.Equal("OLD-1 8h", Show(sheet.RowsOn(thursday)));
        Assert.Equal(thursday, sheet.PreviousWorkedDay(Monday)?.Date);
    }

    [Fact]
    public void Progress_is_the_share_of_the_day_saved()
    {
        var sheet = Sheet(Saved("XYZ-1", 8, Friday), Saved("XYZ-2", 2, Monday), Saved("XYZ-3", 9, Tuesday));
        var saturday = new DateOnly(2026, 10, 17);

        Assert.Equal(1, sheet.Progress(Friday));
        Assert.Equal(0.25, sheet.Progress(Monday));
        Assert.Equal(1, sheet.Progress(Tuesday)); // more than needed is still full
        Assert.Equal(0, sheet.Progress(saturday));
        Assert.True(sheet.IsComplete(Friday));
        Assert.False(sheet.IsComplete(Monday));
        Assert.False(sheet.IsComplete(saturday));
    }

    [Fact]
    public void Without_Jira_hours_are_unknown_so_nothing_is_missing_or_suggested()
    {
        var sheet = new PeriodSheet(Period, () => Tuesday) { Sprint = [Doing] };
        var days = Period.Days.ToDictionary(d => d, d => DayInfo.For(d, new Dictionary<DateOnly, DayOff>()));
        sheet.Load([], [], days, hoursKnown: false);

        Assert.Equal(TimeSpan.Zero, sheet.Missing(Monday));
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

    [Fact]
    public void Taking_a_whole_day_off_marks_its_saved_work_removed_and_keeps_added_rows()
    {
        var sheet = Sheet(Saved("XYZ-1", 6, Monday), Saved("XYZ-2", 2, Monday));
        var added = sheet.WorkFor(Monday).AddRow(new JiraIssue("XYZ-3", ""));

        sheet.TakeOff(Monday);

        var work = sheet.WorkFor(Monday);
        Assert.Equal(2, work.Removed.Count);
        Assert.Contains(added, work.Drafts);
        Assert.Contains(Monday, sheet.UnsavedDays); // Save day deletes the worklogs
    }

    [Fact]
    public void Taking_a_day_off_drops_suggestions_and_undoing_it_brings_the_work_back()
    {
        var sheet = Sheet(Saved("XYZ-1", 2, Monday));
        sheet.Suggest(Monday);
        Assert.NotEmpty(sheet.WorkFor(Monday).Drafts);

        sheet.TakeOff(Monday);
        Assert.Empty(sheet.WorkFor(Monday).Drafts);

        sheet.UndoTakeOff(Monday);
        Assert.Empty(sheet.WorkFor(Monday).Removed);
        Assert.Equal("XYZ-1 6h", Show(sheet.WorkFor(Monday).Drafts)); // suggested again
    }
}
