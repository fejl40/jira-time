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
}
