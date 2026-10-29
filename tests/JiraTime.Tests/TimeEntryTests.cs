using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

public class TimeEntryTests
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private static TimeEntry Entry(string key, double hours) => new() { IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(hours) };

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
        var synced = Entry("ABC-1", 1);
        synced.JiraWorklogId = "10";
        synced.SyncPending = false;
        Assert.False(TimeEntry.HasSyncProblem([synced]));
        Assert.True(TimeEntry.HasSyncProblem([synced, Entry("ABC-2", 1)]));
    }

    [Fact]
    public void Sync_problems_name_each_reason_once()
    {
        var synced = Entry("ABC-1", 1);
        synced.JiraWorklogId = "10";
        synced.SyncPending = false;
        var failed = Entry("ABC-2", 1);
        failed.SyncError = "Jira said no";
        var failedToo = Entry("ABC-3", 1);
        failedToo.SyncError = "Jira said no";

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

        entry.JiraWorklogId = "10";
        entry.JiraWorklogIssueKey = "abc-2";
        Assert.Equal("abc-2", entry.WorklogIssueKey);
        Assert.False(entry.MovedToAnotherIssue); // case doesn't count

        entry.JiraWorklogIssueKey = "ABC-1";
        Assert.Equal("ABC-1", entry.WorklogIssueKey);
        Assert.True(entry.MovedToAnotherIssue);
    }
}

/// <summary>The retry queue as stored: per user, read back after a restart, old formats still readable.</summary>
public class RetryQueueStoreTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TimeHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static TimeEntry Waiting(string key, DateOnly? day = null) =>
        new() { IssueKey = key, Date = day ?? Day, Duration = TimeSpan.FromHours(1), SyncPending = true };

    [Fact]
    public async Task Waiting_changes_in_the_range_come_in_day_order()
    {
        await _h.Queue.AddAsync(Waiting("B", Day.AddDays(1)));
        await _h.Queue.AddAsync(Waiting("A"));
        await _h.Queue.AddAsync(Waiting("X", Day.AddDays(14)));

        Assert.Equal(["A", "B"], (await _h.Queue.GetPendingAsync(Day, Day.AddDays(13))).Select(e => e.IssueKey));
    }

    [Fact]
    public async Task Adding_a_change_with_the_same_id_replaces_it_and_removing_takes_it_out()
    {
        var change = Waiting("ABC-1");
        await _h.Queue.AddAsync(change);
        change.Duration = TimeSpan.FromHours(3);
        await _h.Queue.AddAsync(change);

        Assert.Equal(TimeSpan.FromHours(3), Assert.Single(await _h.Queue.GetPendingAsync(Day, Day)).Duration);
        await _h.Queue.RemoveAsync(change.Id);
        Assert.Empty(await _h.Queue.GetPendingAsync(Day, Day));
    }

    [Fact]
    public async Task Entries_saved_with_start_and_end_times_are_read_as_their_duration()
    {
        var folder = _h.Files.Folder("time-entries");
        await File.WriteAllTextAsync(Path.Combine(folder, "old.json"), $$"""
            { "id": "old", "owner": "{{TestUsers.Carl.Id}}", "issueKey": "ABC-1", "issueSummary": "", "date": "2026-10-19",
              "start": "09:00:00", "end": "11:30:00", "syncPending": true }
            """);

        var queue = _h.Queue;
        var old = Assert.Single(await queue.GetPendingAsync(Day, Day));
        Assert.Equal(TimeSpan.FromHours(2.5), old.Duration);

        await queue.AddAsync(old);
        var json = await File.ReadAllTextAsync(Path.Combine(folder, "old.json"));
        Assert.DoesNotContain("\"start\"", json);
        Assert.Contains("\"duration\"", json);
    }
}
