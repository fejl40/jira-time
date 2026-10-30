using JiraTime.Domain;
using static JiraTime.Tests.Support.TestEntries;

namespace JiraTime.Tests.Domain;

/// <summary>Worklogs as entries, waiting work laid over them, and comparing the two.</summary>
public class WorklogMatchingTests
{
    private static readonly DateOnly Day = new(2026, 10, 19);
    private static readonly JiraWorklog Worklog = new("10001", "ABC-1", "Login page", Day, TimeSpan.FromHours(2), "Review");

    /// <summary>The work of <see cref="Worklog"/> as it was read.</summary>
    private static TimeEntry Entry() => Saved("ABC-1", 2, Day, "Review", worklog: "10001");

    [Fact]
    public void An_entry_matches_its_worklog_when_one_has_no_comment_and_the_other_an_empty_one()
    {
        var e = Entry();
        Assert.True(e.Matches(Worklog));

        e.Comment = null;
        Assert.True(e.Matches(Worklog with { Comment = "" }));
    }

    [Fact]
    public void An_entry_does_not_match_a_worklog_on_another_day_or_with_other_hours_or_comment()
    {
        Assert.False(Entry().Matches(Worklog with { Date = Day.AddDays(1) }));
        Assert.False(Entry().Matches(Worklog with { Duration = TimeSpan.FromHours(3) }));
        Assert.False(Entry().Matches(Worklog with { Comment = "Other" }));
    }

    [Fact]
    public void An_entry_matches_a_worklog_whatever_the_issue() =>
        Assert.True(Entry().Matches(Worklog with { IssueKey = "ABC-2" }));

    [Fact]
    public void A_worklog_becomes_an_entry_that_is_in_Jira()
    {
        var e = Worklog.ToEntry();

        Assert.Equal(("ABC-1", "Login page", Day, TimeSpan.FromHours(2), "Review"), (e.IssueKey, e.IssueSummary, e.Date, e.Duration, e.Comment));
        Assert.Equal("10001", e.JiraWorklogId);
        Assert.Equal("ABC-1", e.JiraWorklogIssueKey);
        Assert.True(e.IsSynced);
        Assert.Equal("jira-10001", e.Id); // the same on every read
    }

    [Fact]
    public void Waiting_work_is_laid_over_Jiras_worklogs()
    {
        var other = Worklog with { Id = "10002", IssueKey = "ABC-2" };
        var gone = Worklog with { Id = "10003", IssueKey = "ABC-3" };
        var changed = Entry();
        changed.Duration = TimeSpan.FromHours(5);
        changed.Failed("Jira said no");
        var removed = Saved("ABC-3", 0, Day, worklog: "10003");
        removed.MarkRemovalPending();
        var added = Waiting("ABC-4", 1, Day.AddDays(-1));

        var merged = WorklogMatching.Merge([Worklog, other, gone, Worklog], [changed, removed, added]);

        Assert.Equal(["ABC-4 1", "ABC-2 2", "ABC-1 5"], merged.Select(e => $"{e.IssueKey} {e.Duration.TotalHours}"));
    }

    [Fact]
    public void Only_a_synced_entry_with_the_same_issue_hours_and_comment_is_the_same_as_in_Jira()
    {
        Assert.True(Entry().SameAsSynced(Entry()));

        var pending = Entry();
        pending.Failed("Jira said no");
        Assert.False(Entry().SameAsSynced(pending));

        var edited = Entry();
        edited.Comment = "Changed";
        Assert.False(edited.SameAsSynced(Entry()));

        edited = Entry();
        edited.IssueKey = "abc-1"; // exact comparison here
        Assert.False(edited.SameAsSynced(Entry()));
    }

    [Fact]
    public void Being_the_same_as_in_Jira_counts_no_comment_as_an_empty_one_like_every_other_comparison()
    {
        var saved = Entry();
        saved.Comment = null;
        var edited = Entry();
        edited.Comment = "";

        Assert.True(edited.SameAsSynced(saved));
        Assert.True(saved.SameAsSynced(edited));
    }
}
