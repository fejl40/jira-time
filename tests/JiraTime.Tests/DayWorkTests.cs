using JiraTime.Domain;

namespace JiraTime.Tests;

public class DayWorkTests
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private static TimeEntry Saved(string key, double hours, string? desc = null) =>
        new() { IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(hours), Description = desc, JiraWorklogId = "1", SyncPending = false };

    [Fact]
    public void Edits_go_to_a_copy_of_the_saved_entry()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);

        work.Edit(a).Duration = TimeSpan.FromHours(6);

        Assert.Equal(TimeSpan.FromHours(4), a.Duration);
        Assert.Equal(TimeSpan.FromHours(6), work.View(a).Duration);
        Assert.True(work.IsChanged(a));
        Assert.True(work.HasChanges([a]));
        Assert.Same(work.Edit(a), work.Edit(a));
        Assert.Same(a, new DayWork(Day).View(a));
    }

    [Fact]
    public void Editing_back_to_the_saved_values_is_no_change()
    {
        var a = Saved("ABC-1", 4, "x");
        var work = new DayWork(Day);
        work.Edit(a).Duration = TimeSpan.FromHours(5);

        work.Edit(a).Duration = TimeSpan.FromHours(4);

        Assert.False(work.HasChanges([a]));
    }

    [Fact]
    public void Removed_rows_are_left_out_of_the_rows_to_save()
    {
        var a = Saved("ABC-1", 4);
        var b = Saved("ABC-2", 4);
        var work = new DayWork(Day);

        work.Removed.Add(a.Id);

        Assert.True(work.HasChanges([a, b]));
        Assert.Equal([b.Id], work.RowsToSave([a, b]).Select(r => r.Id));
        Assert.Equal(TimeSpan.FromHours(4), work.Total([a, b]));
    }

    [Fact]
    public void Blank_new_rows_are_no_change_and_are_not_saved()
    {
        var work = new DayWork(Day);
        work.Drafts.Add(new TimeEntry { IssueKey = "ABC-1", Date = Day });

        Assert.False(work.HasChanges([]));
        Assert.Empty(work.RowsToSave([]));

        work.Drafts[0].Description = "Pairing";
        Assert.True(work.HasChanges([]));
        Assert.Single(work.RowsToSave([]));
    }

    [Fact]
    public void Unreadable_hours_count_as_a_change()
    {
        var work = new DayWork(Day);
        var draft = new TimeEntry { IssueKey = "ABC-1", Date = Day };
        work.Drafts.Add(draft);

        work.BadHours[draft.Id] = "abc";

        Assert.True(work.HasChanges([]));
        Assert.False(work.IsBlank(draft));
    }

    [Fact]
    public void Totals_use_the_edited_hours()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);
        work.Edit(a).Duration = TimeSpan.FromHours(3);
        work.Drafts.Add(new TimeEntry { IssueKey = "ABC-1", Date = Day, Duration = TimeSpan.FromHours(2) });
        work.Drafts.Add(new TimeEntry { IssueKey = "ABC-2", Date = Day, Duration = TimeSpan.FromHours(1) });

        Assert.Equal(TimeSpan.FromHours(6), work.Total([a]));
        Assert.Equal(TimeSpan.FromHours(5), work.TotalFor([a], "ABC-1"));
    }

    [Fact]
    public void Rows_to_save_are_copies()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);

        work.RowsToSave([a])[0].Duration = TimeSpan.FromHours(1);

        Assert.Equal(TimeSpan.FromHours(4), a.Duration);
        Assert.Equal(TimeSpan.FromHours(4), work.View(a).Duration);
    }

    [Fact]
    public void Untouched_suggestions_are_changes_but_not_the_users_own()
    {
        var work = new DayWork(Day);
        var suggestion = new TimeEntry { IssueKey = "ABC-1", Date = Day, Duration = TimeSpan.FromHours(8) };
        work.Drafts.Add(suggestion);
        work.Suggested.Add(suggestion.Id);

        Assert.True(work.HasChanges([]));
        Assert.False(work.HasOwnChanges([]));

        work.Touched(suggestion);
        Assert.True(work.HasOwnChanges([]));
    }

    [Fact]
    public void A_day_with_only_untouched_suggestions_has_only_suggestions()
    {
        var saved = Saved("ABC-1", 4);
        var work = new DayWork(Day);
        Assert.True(work.HasOnlySuggestions([saved]));

        var suggestion = new TimeEntry { IssueKey = "ABC-2", Date = Day, Duration = TimeSpan.FromHours(4) };
        work.AddSuggestions([suggestion]);
        Assert.True(work.HasOnlySuggestions([saved]));

        work.Touched(suggestion);
        Assert.False(work.HasOnlySuggestions([saved]));
    }

    [Fact]
    public void A_changed_or_removed_saved_row_is_more_than_suggestions()
    {
        var saved = Saved("ABC-1", 4);
        var changed = new DayWork(Day);
        changed.Edit(saved).Duration = TimeSpan.FromHours(5);
        var removed = new DayWork(Day);
        removed.Remove(saved, saved);

        Assert.False(changed.HasOnlySuggestions([saved]));
        Assert.False(removed.HasOnlySuggestions([saved]));
    }

    [Fact]
    public void The_row_on_an_issue_skips_removed_rows_and_finds_drafts()
    {
        var saved = Saved("ABC-1", 4);
        var work = new DayWork(Day);
        var draft = work.AddRow(new JiraIssue("ABC-2", ""));

        Assert.Same(saved, work.RowOn([saved], "abc-1"));
        Assert.Same(draft, work.RowOn([saved], "ABC-2"));
        work.Remove(saved, saved);
        Assert.Null(work.RowOn([saved], "ABC-1"));
    }

    [Fact]
    public void Rows_show_each_state()
    {
        var inJira = Saved("ABC-1", 1);
        var changed = Saved("ABC-2", 1);
        var removed = Saved("ABC-3", 1);
        var notInJira = Saved("ABC-4", 1);
        notInJira.SyncPending = true;
        notInJira.SyncError = "Jira said no";
        var work = new DayWork(Day);
        work.Edit(changed).Duration = TimeSpan.FromHours(2);
        work.Remove(removed, removed);
        var added = work.AddRow();
        work.AddSuggestions([new TimeEntry { IssueKey = "ABC-5", Date = Day, Duration = TimeSpan.FromHours(1) }]);

        var rows = work.Rows([inJira, changed, removed, notInJira]);

        Assert.Equal(
            [RowState.InJira, RowState.Changed, RowState.Removed, RowState.NotInJira, RowState.New, RowState.Suggested],
            rows.Select(r => r.State));
        Assert.Equal(TimeSpan.FromHours(2), rows[1].Entry.Duration);
        Assert.Same(added, rows[4].Entry);
        Assert.Null(rows[4].Saved);
    }

    [Fact]
    public void Jira_s_reason_shows_only_on_untouched_rows_Jira_does_not_have()
    {
        var notInJira = Saved("ABC-1", 1);
        notInJira.SyncPending = true;
        notInJira.SyncError = "Jira said no";
        var waiting = Saved("ABC-2", 1);
        waiting.SyncPending = true;
        var work = new DayWork(Day);

        Assert.True(work.Rows([notInJira])[0].ShowsSyncError);
        Assert.False(work.Rows([waiting])[0].ShowsSyncError);

        work.Edit(notInJira).Duration = TimeSpan.FromHours(3);
        Assert.False(work.Rows([notInJira])[0].ShowsSyncError);
    }

    private static TimeEntry Draft(DayWork work, string key = "ABC-1", double hours = 0)
    {
        var draft = work.AddRow(new JiraIssue(key, ""));
        draft.Duration = TimeSpan.FromHours(hours);
        return draft;
    }

    private static TimeEntry Suggestion(DayWork work, string key, double hours)
    {
        var draft = Draft(work, key, hours);
        work.Suggested.Add(draft.Id);
        return draft;
    }

    [Fact]
    public void Editing_a_saved_row_changes_its_copy()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);

        var e = work.Editable(work.View(a), a);

        Assert.NotSame(a, e);
        Assert.Same(work.Edit(a), e);
    }

    [Fact]
    public void Editing_a_suggestion_makes_it_the_users_own_row()
    {
        var work = new DayWork(Day);
        var s = Suggestion(work, "ABC-1", 8);

        Assert.Same(s, work.Editable(s, null));
        Assert.DoesNotContain(s.Id, work.Suggested);
    }

    [Fact]
    public void Picking_an_issue_sets_key_and_summary()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);
        var s = Suggestion(work, "ABC-2", 2);

        var e = work.PickIssue(work.View(a), a, new JiraIssue("XYZ-9", "Other"));
        work.PickIssue(s, null, new JiraIssue("XYZ-8", "More"));

        Assert.Equal(("XYZ-9", "Other"), (e.IssueKey, e.IssueSummary));
        Assert.Equal("ABC-1", a.IssueKey);
        Assert.True(work.IsChanged(a));
        Assert.Equal(("XYZ-8", "More"), (s.IssueKey, s.IssueSummary));
        Assert.Empty(work.Suggested);
    }

    [Fact]
    public void Typed_hours_are_read()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);

        work.SetHours(work.View(a), a, "1:30");

        Assert.Equal(TimeSpan.FromMinutes(90), work.View(a).Duration);
        Assert.Equal(TimeSpan.FromHours(4), a.Duration);
        Assert.Empty(work.BadHours);
    }

    [Fact]
    public void Unreadable_hours_are_kept_and_count_as_zero_until_fixed()
    {
        var work = new DayWork(Day);
        var d = Suggestion(work, "ABC-1", 3);

        work.SetHours(d, null, "abc");

        Assert.Equal("abc", work.BadHours[d.Id]);
        Assert.Equal(TimeSpan.Zero, d.Duration);
        Assert.Empty(work.Suggested);

        work.SetHours(d, null, "2");
        Assert.Empty(work.BadHours);
        Assert.Equal(TimeSpan.FromHours(2), d.Duration);
    }

    [Fact]
    public void Empty_hours_are_zero_and_not_bad()
    {
        var work = new DayWork(Day);
        var d = Draft(work, hours: 3);
        work.SetHours(d, null, "x");

        work.SetHours(d, null, "  ");

        Assert.Equal(TimeSpan.Zero, d.Duration);
        Assert.Empty(work.BadHours);
    }

    [Fact]
    public void Comments_are_trimmed_and_empty_means_none()
    {
        var a = Saved("ABC-1", 4, "old");
        var work = new DayWork(Day);
        var s = Suggestion(work, "ABC-2", 2);

        work.SetDescription(work.View(a), a, "  Pairing  ");
        work.SetDescription(s, null, "   ");

        Assert.Equal("Pairing", work.View(a).Description);
        Assert.Equal("old", a.Description);
        Assert.Null(s.Description);
        Assert.Empty(work.Suggested);
    }

    [Fact]
    public void Added_rows_are_empty_drafts_on_the_day()
    {
        var work = new DayWork(Day);

        var blank = work.AddRow();
        var onIssue = work.AddRow(new JiraIssue("ABC-1", "Login"));

        Assert.Equal([blank, onIssue], work.Drafts);
        Assert.Equal(("", "", Day), (blank.IssueKey, blank.IssueSummary, blank.Date));
        Assert.Equal(("ABC-1", "Login", Day), (onIssue.IssueKey, onIssue.IssueSummary, onIssue.Date));
        Assert.True(work.IsBlank(onIssue));
        Assert.Empty(work.Suggested);
    }

    [Fact]
    public void Fill_the_rest_adds_the_missing_hours()
    {
        var a = Saved("ABC-1", 3);
        var work = new DayWork(Day);
        work.Edit(a).Duration = TimeSpan.FromHours(5);

        work.FillRest([a], TimeSpan.FromHours(8), []);

        var added = Assert.Single(work.Drafts);
        Assert.Equal(TimeSpan.FromHours(3), added.Duration);
        Assert.Equal("ABC-1", added.IssueKey);
        Assert.Equal(TimeSpan.FromHours(8), work.Total([a]));
    }

    [Fact]
    public void Fill_the_rest_adds_nothing_to_a_full_day()
    {
        var a = Saved("ABC-1", 8);
        var work = new DayWork(Day);

        work.FillRest([a], TimeSpan.FromHours(8), []);

        Assert.Empty(work.Drafts);
    }

    [Fact]
    public void Copying_a_day_scales_it_to_the_missing_hours()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);
        var previous = new[] { Saved("XYZ-1", 6), Saved("XYZ-2", 2) };

        work.CopyDay([a], TimeSpan.FromHours(8), previous);

        Assert.Equal([("XYZ-1", TimeSpan.FromHours(3)), ("XYZ-2", TimeSpan.FromHours(1))],
            work.Drafts.Select(d => (d.IssueKey, d.Duration)));
        Assert.All(work.Drafts, d => Assert.Equal(Day, d.Date));
    }

    [Fact]
    public void Copying_a_day_keeps_its_hours_when_none_are_missing()
    {
        var a = Saved("ABC-1", 8);
        var work = new DayWork(Day);

        work.CopyDay([a], TimeSpan.FromHours(8), [Saved("XYZ-1", 6), Saved("XYZ-2", 2)]);

        Assert.Equal([TimeSpan.FromHours(6), TimeSpan.FromHours(2)], work.Drafts.Select(d => d.Duration));
    }

    [Fact]
    public void Copying_a_day_replaces_untouched_suggestions_only()
    {
        var work = new DayWork(Day);
        Suggestion(work, "SUG-1", 8);
        var touched = Suggestion(work, "SUG-2", 2);
        work.Touched(touched);
        var own = Draft(work, "OWN-1", 2);

        work.CopyDay([], TimeSpan.FromHours(8), [Saved("XYZ-1", 8)]);

        Assert.Equal(["SUG-2", "OWN-1", "XYZ-1"], work.Drafts.Select(d => d.IssueKey));
        Assert.Equal(TimeSpan.FromHours(4), work.Drafts[2].Duration);
        Assert.Empty(work.Suggested);
        Assert.Contains(own, work.Drafts);
    }

    [Fact]
    public void Removing_a_saved_row_marks_it_and_restore_keeps_it()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);

        work.Remove(work.View(a), a);
        Assert.True(work.IsRemoved(a));
        Assert.Empty(work.RowsToSave([a]));

        work.Restore(a);
        Assert.False(work.IsRemoved(a));
        Assert.False(work.HasChanges([a]));
    }

    [Fact]
    public void Removing_a_new_row_drops_it_with_its_bad_hours()
    {
        var work = new DayWork(Day);
        var s = Suggestion(work, "ABC-1", 8);
        var d = Draft(work, "ABC-2");
        work.SetHours(d, null, "abc");

        work.Remove(s, null);
        work.Remove(d, null);

        Assert.Empty(work.Drafts);
        Assert.Empty(work.Suggested);
        Assert.Empty(work.BadHours);
        Assert.Empty(work.Removed);
    }
}
