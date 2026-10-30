using JiraTime.Domain;
using static JiraTime.Tests.Support.TestEntries;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Domain;

/// <summary>
/// One day's unsaved changes on their own: edited copies, removals, new rows, suggestions and unreadable hours.
/// <see cref="DayWork"/> is tested in two files: this one, and DayWorkTests.Edits.cs for editing a row's issue, hours and comment.
/// </summary>
public partial class DayWorkTests
{
    private static readonly DateOnly Day = TestEntries.Monday;

    /// <summary>A row the user added.</summary>
    private static TimeEntry AddedRow(DayWork work, string key = "ABC-1", double hours = 0)
    {
        var row = work.AddRow(new JiraIssue(key, ""));
        row.Duration = TimeSpan.FromHours(hours);
        return row;
    }

    /// <summary>A row the app suggested.</summary>
    private static TimeEntry Suggestion(DayWork work, string key, double hours)
    {
        var suggested = Draft(key, hours, Day);
        work.AddSuggestions([suggested]);
        return suggested;
    }

    [Fact]
    public void Edits_go_to_a_copy_of_the_saved_entry()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);

        work.SetHours(a, a, "6");
        var copy = work.View(a);
        work.SetComment(copy, a, "Pairing");

        Assert.Equal(TimeSpan.FromHours(4), a.Duration);
        Assert.Null(a.Comment);
        Assert.NotSame(a, copy);
        Assert.Same(copy, work.View(a)); // one copy, made on first use
        Assert.Equal((TimeSpan.FromHours(6), "Pairing"), (copy.Duration, copy.Comment));
        Assert.True(work.IsChanged(a));
        Assert.Same(a, new DayWork(Day).View(a));
    }

    [Fact]
    public void Editing_back_to_the_saved_values_is_no_change()
    {
        var a = Saved("ABC-1", 4, comment: "x");
        var work = new DayWork(Day);
        work.SetHours(a, a, "5");

        work.SetHours(a, a, "4");

        Assert.False(work.IsChanged(a));
    }

    [Fact]
    public void A_new_row_is_blank_until_it_has_hours_a_comment_or_unreadable_hours()
    {
        var work = new DayWork(Day);
        var commented = work.AddRow();
        var unreadable = work.AddRow();
        Assert.True(work.IsBlank(commented));

        work.SetComment(commented, null, "Pairing");
        work.SetHours(unreadable, null, "abc");

        Assert.False(work.IsBlank(commented));
        Assert.False(work.IsBlank(unreadable));
        Assert.Equal("abc", work.UnreadHours(unreadable.Id));
        Assert.Equal([unreadable.Id], work.BadHourRows);
    }

    [Fact]
    public void Suggestions_count_as_suggestions_until_the_user_changes_them()
    {
        var work = new DayWork(Day);
        var suggestion = Suggestion(work, "ABC-1", 8);
        Assert.True(work.IsSuggested(suggestion));
        Assert.True(work.HasOnlySuggestedDrafts);

        work.SetComment(suggestion, null, "Pairing");

        Assert.False(work.IsSuggested(suggestion));
        Assert.False(work.HasOnlySuggestedDrafts);
    }

    [Fact]
    public void A_removed_saved_row_is_more_than_suggestions()
    {
        var saved = Saved("ABC-1", 4);
        var work = new DayWork(Day);
        Assert.True(work.HasOnlySuggestedDrafts);

        work.Remove(saved, saved);

        Assert.False(work.HasOnlySuggestedDrafts);
    }

    [Fact]
    public void Removing_a_saved_row_marks_it_and_restore_keeps_it()
    {
        var a = Saved("ABC-1", 4);
        var b = Saved("ABC-2", 4);
        var work = new DayWork(Day);

        work.Remove(work.View(a), a);
        work.Remove(b, b);
        Assert.True(work.IsRemoved(a));

        work.Restore(a);
        Assert.False(work.IsRemoved(a));
        work.RestoreAll();
        Assert.False(work.IsRemoved(b));
    }

    [Fact]
    public void Removing_a_new_row_drops_it_with_its_bad_hours()
    {
        var work = new DayWork(Day);
        var s = Suggestion(work, "ABC-1", 8);
        var d = AddedRow(work, "ABC-2");
        work.SetHours(d, null, "abc");

        work.Remove(s, null);
        work.Remove(d, null);

        Assert.Empty(work.Drafts);
        Assert.False(work.IsSuggested(s));
        Assert.Empty(work.BadHourRows);
        Assert.Null(work.UnreadHours(d.Id));
    }

    [Fact]
    public void Dropping_suggestions_keeps_the_users_own_rows()
    {
        var work = new DayWork(Day);
        Suggestion(work, "SUG-1", 8);
        var own = AddedRow(work, "OWN-1", 2);
        var filled = Draft("FIL-1", 1, Day);
        work.AddDrafts([filled]);

        work.DropSuggestions();

        Assert.Equal([own, filled], work.Drafts);
        Assert.False(work.IsSuggested(filled));
    }

    [Fact]
    public void The_new_rows_can_only_be_changed_through_the_work()
    {
        var work = new DayWork(Day);
        AddedRow(work);

        var drafts = Assert.IsAssignableFrom<ICollection<TimeEntry>>(work.Drafts);
        Assert.True(drafts.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => drafts.Add(new TimeEntry { IssueKey = "ABC-2" }));
    }
}
