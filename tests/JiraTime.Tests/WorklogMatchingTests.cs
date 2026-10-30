using JiraTime.Domain;

namespace JiraTime.Tests;

/// <summary>Worklogs as entries, waiting work laid over them, and comparing the two.</summary>
public class WorklogMatchingTests
{
    private static readonly DateOnly Day = new(2026, 10, 19);
    private static readonly JiraWorklog Worklog = new("10001", "ABC-1", "Login page", Day, TimeSpan.FromHours(2), "Review");

    private static TimeEntry Entry() => new()
    {
        IssueKey = "ABC-1",
        IssueSummary = "Old summary",
        Date = Day,
        Duration = TimeSpan.FromHours(2),
        Comment = "Review",
        JiraWorklogId = "10001",
        JiraWorklogIssueKey = "ABC-1",
        SyncPending = false,
    };

    [Fact]
    public void Matches_treats_no_comment_as_empty()
    {
        var e = Entry();
        Assert.True(e.Matches(Worklog));

        e.Comment = null;
        Assert.True(e.Matches(Worklog with { Comment = "" }));
    }

    [Fact]
    public void Matches_spots_a_different_day_hours_or_comment()
    {
        Assert.False(Entry().Matches(Worklog with { Date = Day.AddDays(1) }));
        Assert.False(Entry().Matches(Worklog with { Duration = TimeSpan.FromHours(3) }));
        Assert.False(Entry().Matches(Worklog with { Comment = "Other" }));
    }

    [Fact]
    public void Matches_skips_the_issue_key() =>
        Assert.True(Entry().Matches(Worklog with { IssueKey = "ABC-2" }));

    [Fact]
    public void ToEntry_makes_a_synced_entry_from_Jira()
    {
        var e = Worklog.ToEntry();

        Assert.Equal(("ABC-1", "Login page", Day, TimeSpan.FromHours(2), "Review"), (e.IssueKey, e.IssueSummary, e.Date, e.Duration, e.Comment));
        Assert.Equal("10001", e.JiraWorklogId);
        Assert.Equal("ABC-1", e.JiraWorklogIssueKey);
        Assert.True(e.IsSynced);
        Assert.Equal("jira-10001", e.Id); // the same on every read
    }

    [Fact]
    public void Merge_lays_waiting_work_over_Jiras_worklogs()
    {
        var other = Worklog with { Id = "10002", IssueKey = "ABC-2" };
        var gone = Worklog with { Id = "10003", IssueKey = "ABC-3" };
        var changed = Entry();
        changed.Duration = TimeSpan.FromHours(5);
        changed.Failed("Jira said no");
        var removed = new TimeEntry { IssueKey = "ABC-3", Date = Day, JiraWorklogId = "10003", SyncPending = true, DeletePending = true };
        var added = new TimeEntry { IssueKey = "ABC-4", Date = Day.AddDays(-1), Duration = TimeSpan.FromHours(1), SyncPending = true };

        var merged = WorklogMatching.Merge([Worklog, other, gone, Worklog], [changed, removed, added]);

        Assert.Equal(["ABC-4 1", "ABC-2 2", "ABC-1 5"], merged.Select(e => $"{e.IssueKey} {e.Duration.TotalHours}"));
    }

    [Fact]
    public void SameAsSynced_needs_a_synced_entry_with_the_same_issue_hours_and_comment()
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
    public void SameAsSynced_compares_comments_exactly_so_no_comment_differs_from_an_empty_one()
    {
        var saved = Entry();
        saved.Comment = null;
        var edited = Entry();
        edited.Comment = "";

        Assert.False(edited.SameAsSynced(saved));
        Assert.False(saved.SameAsSynced(edited));
    }
}
