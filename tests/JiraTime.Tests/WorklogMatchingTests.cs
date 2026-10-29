using JiraTime.Domain;

namespace JiraTime.Tests;

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
        Description = "Review",
        JiraWorklogId = "10001",
        JiraWorklogIssueKey = "ABC-1",
        SyncPending = false,
    };

    [Fact]
    public void Matches_treats_no_comment_as_empty()
    {
        var e = Entry();
        Assert.True(e.Matches(Worklog));

        e.Description = null;
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

        Assert.Equal(("ABC-1", "Login page", Day, TimeSpan.FromHours(2), "Review"), (e.IssueKey, e.IssueSummary, e.Date, e.Duration, e.Description));
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
        changed.SyncPending = true;
        var removed = new TimeEntry { IssueKey = "ABC-3", Date = Day, JiraWorklogId = "10003", SyncPending = true, DeletePending = true };
        var added = new TimeEntry { IssueKey = "ABC-4", Date = Day.AddDays(-1), Duration = TimeSpan.FromHours(1), SyncPending = true };

        var merged = WorklogMatching.Merge([Worklog, other, gone, Worklog], [changed, removed, added]);

        Assert.Equal(["ABC-4 1", "ABC-2 2", "ABC-1 5"], merged.Select(e => $"{e.IssueKey} {e.Duration.TotalHours}"));
    }

    [Fact]
    public void SameAsSynced_needs_a_synced_entry_with_the_same_issue_hours_and_description()
    {
        Assert.True(Entry().SameAsSynced(Entry()));

        var pending = Entry();
        pending.SyncPending = true;
        Assert.False(Entry().SameAsSynced(pending));

        var edited = Entry();
        edited.Description = "Changed";
        Assert.False(edited.SameAsSynced(Entry()));

        edited = Entry();
        edited.IssueKey = "abc-1"; // exact comparison here
        Assert.False(edited.SameAsSynced(Entry()));
    }

    [Fact]
    public void A_day_save_puts_rows_on_the_day_and_stops_at_errors()
    {
        var noIssue = new TimeEntry { IssueKey = "", Duration = TimeSpan.FromHours(1) };
        var long1 = new TimeEntry { IssueKey = "ABC-1", Duration = TimeSpan.FromHours(20) };
        var long2 = new TimeEntry { IssueKey = "ABC-2", Duration = TimeSpan.FromHours(5) };

        var plan = DaySave.Plan(Day, [noIssue, long1, long2], [Entry()]);

        Assert.Equal([DaySave.DayError, noIssue.Id], plan.Errors.Keys.Order());
        Assert.Equal(TimeEntry.TooManyHours, plan.Errors[DaySave.DayError]);
        Assert.All([noIssue, long1, long2], r => Assert.Equal(Day, r.Date));
        Assert.Empty(plan.Removed);
        Assert.Empty(plan.ToCheck);
        Assert.Empty(plan.RowsToSend(DateTime.UnixEpoch));
    }

    [Fact]
    public void A_day_save_removes_missing_work_sends_new_and_changed_rows_and_checks_what_Jira_has()
    {
        var kept = Entry();
        var changed = Entry();
        changed.Id = "changed";
        var removed = Entry();
        removed.Id = "removed";
        var removedNotInJira = new TimeEntry { Id = "local", IssueKey = "ABC-9", Date = Day, Duration = TimeSpan.FromHours(1) };
        var edited = changed.Copy();
        edited.Duration = TimeSpan.FromHours(3);
        var added = new TimeEntry { IssueKey = "ABC-4", Duration = TimeSpan.FromHours(1) };

        var plan = DaySave.Plan(Day, [kept.Copy(), edited, added], [kept, changed, removed, removedNotInJira]);

        Assert.Empty(plan.Errors);
        Assert.Equal(["removed", "local"], plan.Removed.Select(e => e.Id));
        Assert.Equal(["removed", "changed"], plan.ToCheck.Select(e => e.Id));
        Assert.Equal([edited, added], plan.RowsToSend(DateTime.UnixEpoch));
    }

    [Fact]
    public void Rows_to_send_take_Jira_bookkeeping_from_what_was_read_and_the_time_given()
    {
        var read = Entry();
        read.CreatedUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var edited = read.Copy();
        edited.Duration = TimeSpan.FromHours(3);
        edited.JiraWorklogId = "tampered";
        edited.JiraWorklogIssueKey = "XYZ-1";
        var now = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

        var row = Assert.Single(DaySave.Plan(Day, [edited], [read]).RowsToSend(now));

        Assert.Equal(("10001", "ABC-1", read.CreatedUtc, now), (row.JiraWorklogId, row.JiraWorklogIssueKey, row.CreatedUtc, row.UpdatedUtc));
    }
}
