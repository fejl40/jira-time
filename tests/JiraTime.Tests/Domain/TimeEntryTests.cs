using JiraTime.Domain;
using static JiraTime.Tests.Support.TestEntries;

namespace JiraTime.Tests.Domain;

public class TimeEntryTests
{
    private static readonly DateOnly Day = Monday;

    /// <summary>Work Jira has as worklog 10 on <paramref name="worklogIssue"/>, which may not be the entry's own issue.</summary>
    private static TimeEntry LoggedOn(string worklogIssue, string key) => new()
    {
        IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(1), JiraWorklogId = "10", JiraWorklogIssueKey = worklogIssue, SyncPending = false,
    };

    [Fact]
    public void A_valid_entry_has_no_error() => Assert.Null(TimeEntry.Validate(Draft("ABC-1", 1.5, Day)));

    [Fact]
    public void An_issue_is_needed() => Assert.NotNull(TimeEntry.Validate(Draft(" ", 1, Day)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Hours_must_be_positive(double hours) => Assert.NotNull(TimeEntry.Validate(Draft("ABC-1", hours, Day)));

    [Fact]
    public void The_hours_of_entries_are_added_up() =>
        Assert.Equal(TimeSpan.FromHours(2.25), TimeEntry.Total([Draft("ABC-1", 1.5, Day), Draft("ABC-2", 0.75, Day)]));

    [Fact]
    public void No_entries_add_up_to_zero_hours() => Assert.Equal(TimeSpan.Zero, TimeEntry.Total([]));

    [Fact]
    public void Entries_not_in_Jira_are_a_sync_problem()
    {
        var synced = Saved("ABC-1", 1, Day, worklog: "10");
        Assert.False(TimeEntry.HasSyncProblem([synced]));
        Assert.True(TimeEntry.HasSyncProblem([synced, Draft("ABC-2", 1, Day)]));
    }

    [Fact]
    public void Sync_problems_name_each_reason_once()
    {
        var synced = Saved("ABC-1", 1, Day, worklog: "10");
        var failed = Draft("ABC-2", 1, Day);
        failed.Failed("Jira said no");
        var failedToo = Draft("ABC-3", 1, Day);
        failedToo.Failed("Jira said no");

        Assert.Empty(TimeEntry.SyncProblems([synced]));
        Assert.Equal(["Jira said no", TimeEntry.NotSentYet], TimeEntry.SyncProblems([synced, failed, failedToo, Draft("ABC-4", 1, Day)]));
    }

    [Fact]
    public void Hours_must_be_whole_minutes() =>
        Assert.NotNull(TimeEntry.Validate(new TimeEntry { IssueKey = "ABC-1", Date = Day, Duration = TimeSpan.FromSeconds(90) }));

    [Fact]
    public void One_entry_has_at_most_24_hours()
    {
        Assert.Null(TimeEntry.Validate(Draft("ABC-1", 24, Day)));
        Assert.Equal(TimeEntry.TooManyHours, TimeEntry.Validate(Draft("ABC-1", 24.5, Day)));
    }

    [Fact]
    public void The_worklog_issue_is_where_it_was_logged_else_the_entrys_own()
    {
        var entry = Draft("ABC-2", 1, Day);
        Assert.Equal("ABC-2", entry.WorklogIssueKey);
        Assert.False(entry.MovedToAnotherIssue); // not in Jira yet

        entry = LoggedOn("abc-2", "ABC-2");
        Assert.Equal("abc-2", entry.WorklogIssueKey);
        Assert.False(entry.MovedToAnotherIssue); // case doesn't count

        entry = LoggedOn("ABC-1", "ABC-2");
        Assert.Equal("ABC-1", entry.WorklogIssueKey);
        Assert.True(entry.MovedToAnotherIssue);
    }

    // ---- Sync transitions ----

    [Fact]
    public void Sent_links_the_entry_to_its_worklog_on_its_own_issue_and_clears_the_error()
    {
        var entry = Draft("ABC-1", 1, Day);
        entry.Failed("Jira said no");

        entry.Sent("42");

        Assert.Equal(("42", "ABC-1", false, null), (entry.JiraWorklogId, entry.JiraWorklogIssueKey, entry.SyncPending, entry.SyncError));
        Assert.True(entry.IsSynced);
    }

    [Fact]
    public void Failed_keeps_the_entry_waiting_with_the_reason()
    {
        var entry = Saved("ABC-1", 1, Day, worklog: "10");

        entry.Failed("Jira said no");

        Assert.Equal((true, "Jira said no", "10"), (entry.SyncPending, entry.SyncError, entry.JiraWorklogId));
        Assert.False(entry.IsSynced);
    }

    [Fact]
    public void A_pending_removal_waits_for_Jira_and_keeps_its_worklog()
    {
        var entry = Saved("ABC-1", 1, Day, worklog: "10");

        entry.MarkRemovalPending();

        Assert.Equal((true, true, "10"), (entry.DeletePending, entry.SyncPending, entry.JiraWorklogId));
    }

    [Fact]
    public void Leaving_the_old_issue_unlinks_the_worklog()
    {
        var entry = LoggedOn("ABC-1", "ABC-2");

        entry.LeftOldIssue();

        Assert.Equal((null, null), (entry.JiraWorklogId, entry.JiraWorklogIssueKey));
        Assert.False(entry.MovedToAnotherIssue);
        Assert.Equal("ABC-2", entry.WorklogIssueKey);
    }

    [Fact]
    public void Taking_the_worklog_from_what_was_read_takes_its_link_and_creation_time()
    {
        var read = Saved("ABC-1", 1, Day, worklog: "10");
        read.CreatedUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var edited = Draft("ABC-2", 3, Day);

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
        var a = Draft("ABC-1", 2, Day);
        var b = Draft("ABC-1", 2, Day);
        b.Comment = "";
        Assert.True(a.SameWork(b));
        Assert.True(b.SameWork(a));

        b.Comment = "Review";
        Assert.False(a.SameWork(b));
        Assert.False(a.SameWork(Draft("ABC-2", 2, Day)));
        Assert.False(a.SameWork(Draft("ABC-1", 3, Day)));
    }

    [Fact]
    public void New_entries_have_no_time_stamps_until_saved()
    {
        var entry = Draft("ABC-1", 1, Day);
        Assert.Equal((default(DateTime), default(DateTime)), (entry.CreatedUtc, entry.UpdatedUtc));
    }

    [Fact]
    public void The_comment_is_stored_as_description()
    {
        var entry = Draft("ABC-1", 1, Day);
        entry.Comment = "Review";
        var json = System.Text.Json.JsonSerializer.Serialize(entry, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.Contains("\"description\":\"Review\"", json);
        var read = System.Text.Json.JsonSerializer.Deserialize<TimeEntry>(json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
        Assert.Equal("Review", read.Comment);
    }
}
