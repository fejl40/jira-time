using JiraTime.Domain;

namespace JiraTime.Tests;

public class TimeEntryTests
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private static TimeEntry Entry(string key, double hours) => new() { IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(hours) };

    private static TimeEntry InJira(string key, double hours, string worklogIssue = "") => new()
    {
        IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(hours),
        JiraWorklogId = "10", JiraWorklogIssueKey = worklogIssue == "" ? key : worklogIssue, SyncPending = false,
    };

    [Fact]
    public void A_valid_entry_has_no_error() => Assert.Null(TimeEntry.Validate(Entry("ABC-1", 1.5)));

    [Fact]
    public void An_issue_is_needed() => Assert.NotNull(TimeEntry.Validate(Entry(" ", 1)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Hours_must_be_positive(double hours) => Assert.NotNull(TimeEntry.Validate(Entry("ABC-1", hours)));

    [Fact]
    public void Total_adds_up_the_hours_of_the_entries() =>
        Assert.Equal(TimeSpan.FromHours(2.25), TimeEntry.Total([Entry("ABC-1", 1.5), Entry("ABC-2", 0.75)]));

    [Fact]
    public void Total_of_no_entries_is_zero() => Assert.Equal(TimeSpan.Zero, TimeEntry.Total([]));

    [Fact]
    public void Entries_not_in_Jira_are_a_sync_problem()
    {
        var synced = InJira("ABC-1", 1);
        Assert.False(TimeEntry.HasSyncProblem([synced]));
        Assert.True(TimeEntry.HasSyncProblem([synced, Entry("ABC-2", 1)]));
    }

    [Fact]
    public void Sync_problems_name_each_reason_once()
    {
        var synced = InJira("ABC-1", 1);
        var failed = Entry("ABC-2", 1);
        failed.Failed("Jira said no");
        var failedToo = Entry("ABC-3", 1);
        failedToo.Failed("Jira said no");

        Assert.Empty(TimeEntry.SyncProblems([synced]));
        Assert.Equal(["Jira said no", TimeEntry.NotSentYet], TimeEntry.SyncProblems([synced, failed, failedToo, Entry("ABC-4", 1)]));
    }

    [Fact]
    public void Hours_must_be_whole_minutes() =>
        Assert.NotNull(TimeEntry.Validate(new TimeEntry { IssueKey = "ABC-1", Date = Day, Duration = TimeSpan.FromSeconds(90) }));

    [Fact]
    public void One_entry_has_at_most_24_hours()
    {
        Assert.Null(TimeEntry.Validate(Entry("ABC-1", 24)));
        Assert.Equal(TimeEntry.TooManyHours, TimeEntry.Validate(Entry("ABC-1", 24.5)));
    }

    [Fact]
    public void The_worklog_issue_is_where_it_was_logged_else_the_entrys_own()
    {
        var entry = Entry("ABC-2", 1);
        Assert.Equal("ABC-2", entry.WorklogIssueKey);
        Assert.False(entry.MovedToAnotherIssue); // not in Jira yet

        entry = InJira("ABC-2", 1, worklogIssue: "abc-2");
        Assert.Equal("abc-2", entry.WorklogIssueKey);
        Assert.False(entry.MovedToAnotherIssue); // case doesn't count

        entry = InJira("ABC-2", 1, worklogIssue: "ABC-1");
        Assert.Equal("ABC-1", entry.WorklogIssueKey);
        Assert.True(entry.MovedToAnotherIssue);
    }

    // ---- Sync transitions ----

    [Fact]
    public void Sent_links_the_entry_to_its_worklog_on_its_own_issue_and_clears_the_error()
    {
        var entry = Entry("ABC-1", 1);
        entry.Failed("Jira said no");

        entry.Sent("42");

        Assert.Equal(("42", "ABC-1", false, null), (entry.JiraWorklogId, entry.JiraWorklogIssueKey, entry.SyncPending, entry.SyncError));
        Assert.True(entry.IsSynced);
    }

    [Fact]
    public void Failed_keeps_the_entry_waiting_with_the_reason()
    {
        var entry = InJira("ABC-1", 1);

        entry.Failed("Jira said no");

        Assert.Equal((true, "Jira said no", "10"), (entry.SyncPending, entry.SyncError, entry.JiraWorklogId));
        Assert.False(entry.IsSynced);
    }

    [Fact]
    public void A_pending_removal_waits_for_Jira_and_keeps_its_worklog()
    {
        var entry = InJira("ABC-1", 1);

        entry.MarkRemovalPending();

        Assert.Equal((true, true, "10"), (entry.DeletePending, entry.SyncPending, entry.JiraWorklogId));
    }

    [Fact]
    public void Leaving_the_old_issue_unlinks_the_worklog()
    {
        var entry = InJira("ABC-2", 1, worklogIssue: "ABC-1");

        entry.LeftOldIssue();

        Assert.Equal((null, null), (entry.JiraWorklogId, entry.JiraWorklogIssueKey));
        Assert.False(entry.MovedToAnotherIssue);
        Assert.Equal("ABC-2", entry.WorklogIssueKey);
    }

    [Fact]
    public void Taking_the_worklog_from_what_was_read_takes_its_link_and_creation_time()
    {
        var read = InJira("ABC-1", 1);
        read.CreatedUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var edited = Entry("ABC-2", 3);

        edited.TakeWorklogFrom(read);

        Assert.Equal(("10", "ABC-1", read.CreatedUtc), (edited.JiraWorklogId, edited.JiraWorklogIssueKey, edited.CreatedUtc));
        Assert.Equal(("ABC-2", TimeSpan.FromHours(3)), (edited.IssueKey, edited.Duration)); // the edit itself stays
    }

    // ---- Comments and same work ----

    [Theory]
    [InlineData("  Pairing ", "Pairing")]
    [InlineData("   ", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void A_typed_comment_is_trimmed_and_blank_means_none(string? typed, string? kept) =>
        Assert.Equal(kept, TimeEntry.CleanComment(typed));

    [Fact]
    public void No_comment_is_the_same_comment_as_an_empty_one()
    {
        Assert.True(TimeEntry.SameComment(null, ""));
        Assert.True(TimeEntry.SameComment("Review", "Review"));
        Assert.False(TimeEntry.SameComment("Review", "review"));
        Assert.False(TimeEntry.SameComment(" Review", "Review"));
    }

    [Fact]
    public void Same_work_means_same_issue_hours_and_comment()
    {
        var a = Entry("ABC-1", 2);
        var b = Entry("ABC-1", 2);
        b.Comment = "";
        Assert.True(a.SameWork(b));
        Assert.True(b.SameWork(a));

        b.Comment = "Review";
        Assert.False(a.SameWork(b));
        Assert.False(a.SameWork(Entry("ABC-2", 2)));
        Assert.False(a.SameWork(Entry("ABC-1", 3)));
    }

    [Fact]
    public void New_entries_have_no_time_stamps_until_saved()
    {
        var entry = Entry("ABC-1", 1);
        Assert.Equal((default(DateTime), default(DateTime)), (entry.CreatedUtc, entry.UpdatedUtc));
    }

    [Fact]
    public void The_comment_is_stored_as_description()
    {
        var entry = Entry("ABC-1", 1);
        entry.Comment = "Review";
        var json = System.Text.Json.JsonSerializer.Serialize(entry, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.Contains("\"description\":\"Review\"", json);
        var read = System.Text.Json.JsonSerializer.Deserialize<TimeEntry>(json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
        Assert.Equal("Review", read.Comment);
    }
}
