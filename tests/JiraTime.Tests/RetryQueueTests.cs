using System.Net;
using JiraTime.Domain;
using static JiraTime.Tests.TimeHarness;

namespace JiraTime.Tests;

/// <summary>Changes Jira couldn't take wait in a local queue, show as not in Jira and are sent by Sync now.</summary>
public class RetryQueueTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);
    private static readonly DateOnly From = new(2026, 10, 16), To = new(2026, 10, 31);

    private readonly TimeHarness _h = new();
    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task When_Jira_is_down_the_day_is_queued_shown_as_not_in_Jira_and_counted()
    {
        _h.Jira.Offline = true;

        var result = await _h.SaveDayAsync(Day, [Row("ABC-1", 6), Row("ABC-2", 2)]);

        Assert.True(result.Saved);
        Assert.Equal(2, result.SyncFailures);
        var queued = await _h.QueuedAsync();
        Assert.Equal(2, queued.Count);
        Assert.All(queued, e => { Assert.False(e.IsSynced); Assert.NotNull(e.SyncError); });

        var time = await _h.Log.GetRangeAsync(From, To);
        Assert.False(time.IsComplete); // Jira can't be read either
        Assert.Equal(TimeSpan.FromHours(8), TimeEntry.Total(time.Entries));
    }

    [Fact]
    public async Task Sync_sends_the_queue_and_empties_it()
    {
        _h.Jira.Offline = true;
        await _h.SaveDayAsync(Day, [Row("ABC-1", 6)]);
        _h.Jira.Offline = false;

        var stillWaiting = await _h.Sync.SyncRangeAsync(_h.Connection, From, To);

        Assert.Equal(0, stillWaiting);
        Assert.Empty(await _h.QueuedAsync());
        Assert.Equal(6 * 3600, _h.JiraSeconds("ABC-1"));
        Assert.True(Assert.Single(await _h.DayAsync(Day)).IsSynced);
    }

    [Fact]
    public async Task The_queue_survives_a_restart()
    {
        _h.Jira.Offline = true;
        await _h.SaveDayAsync(Day, [Row("ABC-1", 6)]);

        var again = new JiraTime.Services.RetryQueue(_h.Files.Reopen(), TestUsers.SignedIn());

        Assert.Single(await again.GetPendingAsync(From, To));
    }

    [Fact]
    public async Task Only_the_changes_Jira_refused_are_queued()
    {
        _h.Jira.FailWhen = uri => uri.AbsolutePath.Contains("ABC-2") ? HttpStatusCode.InternalServerError : null;

        var result = await _h.SaveDayAsync(Day, [Row("ABC-1", 6), Row("ABC-2", 2)]);

        Assert.Equal(1, result.SyncFailures);
        Assert.Equal(6 * 3600, _h.JiraSeconds("ABC-1"));
        Assert.Equal("ABC-2", Assert.Single(await _h.QueuedAsync()).IssueKey);
        _h.Jira.FailWhen = null;
        Assert.Equal(["ABC-1 True", "ABC-2 False"], (await _h.DayAsync(Day)).Select(e => $"{e.IssueKey} {e.IsSynced}").Order());
    }

    [Fact]
    public async Task A_failed_removal_hides_the_work_and_is_retried()
    {
        await _h.SaveDayAsync(Day, [Row("ABC-1", 6), Row("ABC-2", 2)]);
        var rows = (await _h.CopiesAsync(Day)).Where(r => r.IssueKey == "ABC-1").ToList();
        // The check before saving reads the worklog, which works; only the delete fails.
        var failDeletes = true;
        _h.Jira.FailWhen = _ => failDeletes && _h.Jira.Requests[^1].Method == HttpMethod.Delete ? HttpStatusCode.InternalServerError : null;

        var result = await _h.SaveDayAsync(Day, rows);

        Assert.Equal(1, result.SyncFailures);
        Assert.Equal(2 * 3600, _h.JiraSeconds("ABC-2")); // still in Jira
        var queued = Assert.Single(await _h.QueuedAsync());
        Assert.True(queued.DeletePending);
        Assert.Equal(["ABC-1"], (await _h.DayAsync(Day)).Select(e => e.IssueKey)); // hidden here

        failDeletes = false;
        Assert.Equal(0, await _h.Sync.SyncRangeAsync(_h.Connection, From, To));
        Assert.Equal(0, _h.JiraSeconds("ABC-2"));
        Assert.Empty(await _h.QueuedAsync());
    }

    [Fact]
    public async Task Editing_queued_work_again_replaces_it_in_the_queue()
    {
        _h.Jira.Offline = true;
        await _h.SaveDayAsync(Day, [Row("ABC-1", 6)]);
        var rows = await _h.CopiesAsync(Day);
        rows[0].Duration = TimeSpan.FromHours(7);

        await _h.SaveDayAsync(Day, rows);

        Assert.Equal(TimeSpan.FromHours(7), Assert.Single(await _h.QueuedAsync()).Duration);
        _h.Jira.Offline = false;
        await _h.Sync.SyncRangeAsync(_h.Connection, From, To);
        Assert.Equal(7 * 3600, _h.JiraSeconds("ABC-1"));
        Assert.Single(_h.Jira.Worklogs["ABC-1"]);
    }

    [Fact]
    public async Task A_queued_edit_of_a_Jira_worklog_updates_that_worklog_when_sent()
    {
        var id = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var rows = await _h.CopiesAsync(Day);
        rows[0].Duration = TimeSpan.FromHours(2);
        _h.Jira.FailWhen = uri => _h.Jira.Requests[^1].Method == HttpMethod.Put ? HttpStatusCode.BadGateway : null;

        Assert.Equal(1, (await _h.SaveDayAsync(Day, rows)).SyncFailures);
        var queued = Assert.Single(await _h.QueuedAsync());
        Assert.Equal(id, queued.JiraWorklogId);
        Assert.DoesNotContain("jira-", queued.Id); // queued work has its own id
        Assert.Equal(TimeSpan.FromHours(2), Assert.Single(await _h.DayAsync(Day)).Duration); // laid over Jira's

        _h.Jira.FailWhen = null;
        await _h.Sync.SyncRangeAsync(_h.Connection, From, To);
        Assert.Equal(2 * 3600, _h.JiraSeconds("ABC-1"));
        Assert.Single(_h.Jira.Worklogs["ABC-1"]);
    }

    [Fact]
    public async Task A_queued_edit_whose_worklog_was_deleted_in_Jira_is_logged_again()
    {
        var id = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        await _h.Queue.AddAsync(new TimeEntry
        {
            IssueKey = "ABC-1", Date = Day, Duration = TimeSpan.FromHours(2), JiraWorklogId = id, JiraWorklogIssueKey = "ABC-1", SyncPending = true,
        });
        _h.Jira.Worklogs["ABC-1"].Remove(id);

        await _h.Sync.SyncRangeAsync(_h.Connection, From, To);

        Assert.Equal(2 * 3600, _h.JiraSeconds("ABC-1"));
    }

    [Fact]
    public async Task Removing_queued_work_that_never_reached_Jira_just_drops_it()
    {
        _h.Jira.Offline = true;
        await _h.SaveDayAsync(Day, [Row("ABC-1", 6)]);
        var saved = await _h.DayAsync(Day);
        _h.Jira.Offline = false;
        _h.Jira.Requests.Clear();

        Assert.True((await _h.SaveDayAsync(Day, [], saved)).Saved);

        Assert.Empty(await _h.QueuedAsync());
        Assert.DoesNotContain(_h.Jira.Requests, r => r.Method != HttpMethod.Get);
    }
}
