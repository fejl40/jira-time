using JiraTime.Domain;
using static JiraTime.Tests.DayWorkTests;

namespace JiraTime.Tests;

/// <summary>Editing a row of a day: its issue, hours and comment, and adding rows.</summary>
public class DayWorkEditTests
{
    [Fact]
    public void Editing_a_suggestion_changes_the_row_itself_and_makes_it_the_users_own()
    {
        var work = new DayWork(Day);
        var s = Suggestion(work, "ABC-1", 8);

        work.SetHours(s, null, "6");

        Assert.Equal(TimeSpan.FromHours(6), s.Duration);
        Assert.False(work.IsSuggested(s));
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
        Assert.False(work.IsSuggested(s));
    }

    [Fact]
    public void Typed_hours_are_read()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);

        work.SetHours(work.View(a), a, "1:30");

        Assert.Equal(TimeSpan.FromMinutes(90), work.View(a).Duration);
        Assert.Equal(TimeSpan.FromHours(4), a.Duration);
        Assert.Empty(work.BadHourRows);
    }

    [Fact]
    public void Unreadable_hours_are_kept_and_count_as_zero_until_fixed()
    {
        var work = new DayWork(Day);
        var d = Suggestion(work, "ABC-1", 3);

        work.SetHours(d, null, "abc");

        Assert.Equal("abc", work.UnreadHours(d.Id));
        Assert.Equal(TimeSpan.Zero, d.Duration);
        Assert.False(work.IsSuggested(d));

        work.SetHours(d, null, "2");
        Assert.Empty(work.BadHourRows);
        Assert.Equal(TimeSpan.FromHours(2), d.Duration);
    }

    [Fact]
    public void Unreadable_hours_on_a_saved_row_are_kept_by_its_id()
    {
        var a = Saved("ABC-1", 4);
        var work = new DayWork(Day);

        work.SetHours(a, a, "lots");

        Assert.Equal("lots", work.UnreadHours(a.Id));
        Assert.Equal(TimeSpan.FromHours(4), a.Duration);
        Assert.Equal(TimeSpan.Zero, work.View(a).Duration);
    }

    [Fact]
    public void Empty_hours_are_zero_and_not_bad()
    {
        var work = new DayWork(Day);
        var d = Draft(work, hours: 3);
        work.SetHours(d, null, "x");

        work.SetHours(d, null, "  ");

        Assert.Equal(TimeSpan.Zero, d.Duration);
        Assert.Empty(work.BadHourRows);
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
        Assert.False(work.IsSuggested(s));
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
        Assert.False(work.IsSuggested(onIssue));
    }
}
