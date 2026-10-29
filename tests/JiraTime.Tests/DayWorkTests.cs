using JiraTime.Domain;

namespace JiraTime.Tests;

/// <summary>One day's unsaved changes: what counts as a change, totals, rows to save, removing and row states.</summary>
public class DayWorkTests
{
    internal static readonly DateOnly Day = new(2026, 10, 19);

    /// <summary>A row Jira has.</summary>
    internal static TimeEntry Saved(string key, double hours, string? desc = null) =>
        new() { IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(hours), Description = desc, JiraWorklogId = "1", SyncPending = false };

    /// <summary>A row the user added.</summary>
    internal static TimeEntry Draft(DayWork work, string key = "ABC-1", double hours = 0)
    {
        var draft = work.AddRow(new JiraIssue(key, ""));
        draft.Duration = TimeSpan.FromHours(hours);
        return draft;
    }

    /// <summary>A row the app suggested.</summary>
    internal static TimeEntry Suggestion(DayWork work, string key, double hours)
    {
        var draft = Draft(work, key, hours);
        work.Suggested.Add(draft.Id);
        return draft;
    }

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
