using JiraTime.Domain;
using static JiraTime.Tests.DayWorkTests;

namespace JiraTime.Tests;

/// <summary>A day of the sheet: its rows and totals, what counts as a change, filling, copying and why it can't be saved.</summary>
public class SheetDayTests
{
    /// <summary>A sheet with <paramref name="saved"/> on <see cref="DayWorkTests.Day"/> (Monday 19 October, also today).</summary>
    internal static PeriodSheet Sheet(params TimeEntry[] saved)
    {
        var sheet = new PeriodSheet(Period.For(Day), () => Day);
        var days = sheet.Period.Days.ToDictionary(d => d, d => DayInfo.For(d, new Dictionary<DateOnly, DayOff>()));
        sheet.Load(saved, [], days);
        return sheet;
    }

    internal static SheetDay DayWith(params TimeEntry[] saved) => Sheet(saved).Day(Day);

    [Fact]
    public void Rows_show_each_state()
    {
        var inJira = Saved("ABC-1", 1);
        var changed = Saved("ABC-2", 1);
        var removed = Saved("ABC-3", 1);
        var notInJira = new TimeEntry { IssueKey = "ABC-4", IssueSummary = "ABC-4 summary", Date = Day, Duration = TimeSpan.FromHours(1), JiraWorklogId = "1" };
        var sheet = Sheet(inJira, changed, removed, notInJira);
        sheet.Suggest(Day);
        var day = sheet.Day(Day);
        day.SetHours(changed, "2");
        day.Remove(removed);
        var added = day.AddRow();

        var rows = day.Rows;

        Assert.Equal(
            [RowState.InJira, RowState.Changed, RowState.Removed, RowState.NotInJira, RowState.Suggested, RowState.New],
            rows.Select(r => r.State));
        Assert.Equal(TimeSpan.FromHours(2), rows[1].Entry.Duration);
        Assert.Same(added, rows[5].Entry);
        Assert.Null(rows[5].Saved);
    }

    [Fact]
    public void Jira_s_reason_shows_only_on_untouched_rows_Jira_does_not_have()
    {
        var notInJira = Saved("ABC-1", 1);
        notInJira.Failed("Jira said no");
        var waiting = new TimeEntry { IssueKey = "ABC-2", IssueSummary = "ABC-2 summary", Date = Day, Duration = TimeSpan.FromHours(1), JiraWorklogId = "1" };
        var day = DayWith(notInJira, waiting);

        Assert.Equal([true, false], day.Rows.Select(r => r.ShowsSyncError));

        day.SetHours(notInJira, "3");
        Assert.False(day.Rows[0].ShowsSyncError);
    }

    [Fact]
    public void Removed_rows_are_left_out_of_the_rows_to_save_and_undo_brings_them_back()
    {
        var a = Saved("ABC-1", 4);
        var b = Saved("ABC-2", 4);
        var day = DayWith(a, b);

        day.Remove(day.Rows[0].Entry);

        Assert.True(day.HasChanges);
        Assert.Equal([b.Id], day.RowsToSave.Select(r => r.Id));
        Assert.Equal(TimeSpan.FromHours(4), day.Total);

        day.Restore(day.Rows[0].Entry);
        Assert.False(day.HasChanges);
    }

    [Fact]
    public void Totals_use_the_edited_hours_and_blank_new_rows_are_left_out()
    {
        var a = Saved("ABC-1", 4);
        var day = DayWith(a);
        day.SetHours(a, "3");
        day.SetHours(day.AddRow(new JiraIssue("ABC-1", "")), "2");
        day.SetHours(day.AddRow(new JiraIssue("ABC-2", "")), "1");
        day.AddRow(new JiraIssue("ABC-3", ""));

        Assert.Equal(TimeSpan.FromHours(6), day.Total);
        Assert.Equal(TimeSpan.FromHours(5), day.TotalFor("ABC-1"));
        Assert.Equal(3, day.RowsToSave.Count);
        Assert.Equal(TimeSpan.FromHours(4), day.Logged);
    }

    [Fact]
    public void Rows_to_save_are_copies()
    {
        var a = Saved("ABC-1", 4);
        var day = DayWith(a);

        day.RowsToSave[0].Duration = TimeSpan.FromHours(1);

        Assert.Equal(TimeSpan.FromHours(4), a.Duration);
        Assert.Equal(TimeSpan.FromHours(4), day.Rows[0].Entry.Duration);
    }

    [Fact]
    public void Untouched_suggestions_are_changes_but_not_the_users_own()
    {
        var sheet = Sheet();
        sheet.Suggest(Day);
        var day = sheet.Day(Day);

        Assert.True(day.HasChanges);
        Assert.False(day.HasOwnChanges);
        Assert.True(day.HasOnlySuggestions);

        day.SetComment(day.Rows[0].Entry, "Pairing");
        Assert.True(day.HasOwnChanges);
        Assert.False(day.HasOnlySuggestions);
    }

    [Fact]
    public void A_changed_saved_row_is_more_than_suggestions_and_unreadable_hours_are_a_change()
    {
        var saved = Saved("ABC-1", 4);
        var day = DayWith(saved);
        Assert.True(day.HasOnlySuggestions);

        day.SetHours(saved, "abc");

        Assert.False(day.HasOnlySuggestions);
        Assert.True(day.HasOwnChanges);
        Assert.Equal("abc", day.UnreadHours(saved.Id));
    }

    [Fact]
    public void Fill_the_rest_adds_the_missing_hours_and_nothing_to_a_full_day()
    {
        var a = Saved("ABC-1", 3);
        var day = DayWith(a);
        day.SetHours(a, "5");

        day.FillRest();

        var added = day.Rows[^1];
        Assert.Equal((RowState.New, "ABC-1", TimeSpan.FromHours(3)), (added.State, added.Entry.IssueKey, added.Entry.Duration));
        Assert.Equal(TimeSpan.FromHours(8), day.Total);
        day.FillRest();
        Assert.Equal(2, day.Rows.Count);
    }

    [Fact]
    public void Copying_a_day_scales_it_to_the_missing_hours()
    {
        var day = DayWith(Saved("ABC-1", 4));

        day.CopyDay([Saved("XYZ-1", 6), Saved("XYZ-2", 2)]);

        var copied = day.Rows.Where(r => r.IsDraft).Select(r => r.Entry).ToList();
        Assert.Equal([("XYZ-1", TimeSpan.FromHours(3)), ("XYZ-2", TimeSpan.FromHours(1))], copied.Select(d => (d.IssueKey, d.Duration)));
        Assert.All(copied, d => Assert.Equal(Day, d.Date));
    }

    [Fact]
    public void Copying_a_day_keeps_its_hours_when_none_are_missing()
    {
        var day = DayWith(Saved("ABC-1", 8));

        day.CopyDay([Saved("XYZ-1", 6), Saved("XYZ-2", 2)]);

        Assert.Equal([TimeSpan.FromHours(6), TimeSpan.FromHours(2)], day.Rows.Where(r => r.IsDraft).Select(r => r.Entry.Duration));
    }

    [Fact]
    public void Copying_a_day_replaces_untouched_suggestions_only()
    {
        var sheet = Sheet();
        sheet.Suggest(Day);
        var day = sheet.Day(Day);
        var own = day.AddRow(new JiraIssue("OWN-1", ""));
        day.SetHours(own, "2");

        day.CopyDay([Saved("XYZ-1", 8)]);

        Assert.Equal([("OWN-1", RowState.New), ("XYZ-1", RowState.New)], day.Rows.Select(r => (r.Entry.IssueKey, r.State)));
        Assert.Equal(TimeSpan.FromHours(6), day.Rows[1].Entry.Duration);
    }

    [Fact]
    public void The_row_on_an_issue_skips_removed_rows_and_finds_new_ones()
    {
        var saved = Saved("ABC-1", 4);
        var day = DayWith(saved);
        var draft = day.AddRow(new JiraIssue("ABC-2", ""));

        Assert.Same(saved, day.RowOn("abc-1"));
        Assert.Same(draft, day.RowOn("ABC-2"));
        day.Remove(saved);
        Assert.Null(day.RowOn("ABC-1"));
    }

    [Fact]
    public void Errors_cover_rows_that_need_an_issue_and_hours_that_cannot_be_read()
    {
        var day = DayWith();
        var noIssue = day.AddRow();
        day.SetHours(noIssue, "2");
        var unreadable = day.AddRow(new JiraIssue("ABC-1", ""));
        day.SetHours(unreadable, "lots");

        Assert.Equal("Pick a Jira issue.", day.Errors[noIssue.Id]);
        Assert.Equal(DaySave.BadHours, day.Errors[unreadable.Id]);
        Assert.Equal(day.Errors, day.Plan().Errors);
    }

    [Fact]
    public void Computing_errors_and_planning_leave_the_day_and_its_saved_rows_unchanged()
    {
        var kept = Saved("ABC-1", 4);
        var gone = Saved("ABC-2", 1);
        var day = DayWith(kept, gone);
        day.SetHours(kept, "5");
        day.Remove(gone);
        var draft = day.AddRow(new JiraIssue("ABC-3", ""));
        day.SetHours(draft, "2");
        draft.Date = Day.AddDays(1); // the plan puts its copy on the day
        var before = Show(day.Rows) + draft.Date;

        _ = day.Errors;
        var plan = day.Plan();
        var sent = plan.RowsToSend(DateTime.UnixEpoch);

        Assert.Equal(before, Show(day.Rows) + draft.Date);
        Assert.Equal((TimeSpan.FromHours(4), false), (kept.Duration, gone.DeletePending));
        Assert.All(sent.Concat(plan.Removed), r => Assert.DoesNotContain(day.Rows, w => ReferenceEquals(w.Entry, r) || ReferenceEquals(w.Saved, r)));
        Assert.All(sent, r => Assert.Equal(Day, r.Date));
    }

    private static string Show(IEnumerable<WorkRow> rows) =>
        string.Join(", ", rows.Select(r => $"{r.Entry.IssueKey} {r.Entry.Duration.TotalHours}h {r.State} {r.Entry.Date} {r.Entry.UpdatedUtc:O}"));
}
