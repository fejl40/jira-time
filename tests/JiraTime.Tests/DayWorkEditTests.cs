using JiraTime.Domain;
using static JiraTime.Tests.DayWorkTests;

namespace JiraTime.Tests;

/// <summary>Editing a day: changing rows, adding rows, filling the rest and copying another day.</summary>
public class DayWorkEditTests
{
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
}
