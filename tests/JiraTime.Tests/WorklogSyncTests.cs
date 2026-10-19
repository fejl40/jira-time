using System.Net;
using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage.FileSystem;

namespace JiraTime.Tests;

public class WorklogSyncTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);
    private static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token", null);
    private static readonly JiraConnection Cloud = new("https://x.atlassian.net", JiraAuthType.Cloud, "token", "me@example.com");

    private readonly TempDir _dir = new();
    private readonly FileDocumentStore _store;
    private readonly TimeEntryService _entries;
    private readonly FakeJira _jira = new();
    private readonly WorklogSync _sync;

    public WorklogSyncTests()
    {
        _store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = _dir.Path });
        _entries = new TimeEntryService(_store);
        _sync = new WorklogSync(new JiraClient(_jira.Client()), _entries);
    }

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }

    private static TimeEntry Entry(string key = "ABC-1", string start = "09:00", string end = "10:30", string? desc = "Did things") =>
        new() { IssueKey = key, Date = Day, Start = TimeOnly.Parse(start), End = TimeOnly.Parse(end), Description = desc };

    private async Task<TimeEntry> Stored(string id) => (await _entries.GetAsync(id))!;

    [Fact]
    public async Task Saving_a_new_entry_creates_a_worklog_and_marks_it_synced()
    {
        var entry = Entry();

        var result = await _sync.SaveAsync(Server, entry);

        Assert.True(result.Saved);
        Assert.Null(result.SyncError);
        var stored = await Stored(entry.Id);
        Assert.True(stored.IsSynced);
        Assert.Equal("ABC-1", stored.JiraWorklogIssueKey);

        var log = _jira.Worklogs["ABC-1"][stored.JiraWorklogId!];
        Assert.Equal(5400, (int)log["timeSpentSeconds"]!);
        Assert.Equal("Did things", (string)log["comment"]!);
        Assert.StartsWith("2026-10-19T09:00:00.000", (string)log["started"]!);
        Assert.Contains(_jira.Requests, r => r.Method == HttpMethod.Post && r.Path.StartsWith("/rest/api/2/issue/ABC-1/worklog"));
    }

    [Fact]
    public async Task Cloud_uses_api_v3_and_a_document_comment()
    {
        var entry = Entry();

        await _sync.SaveAsync(Cloud, entry);

        var log = _jira.Worklogs["ABC-1"].Values.Single();
        Assert.Equal("doc", (string)log["comment"]!["type"]!);
        Assert.Equal("Did things", (string)log["comment"]!["content"]![0]!["content"]![0]!["text"]!);
        Assert.Contains(_jira.Requests, r => r.Path.StartsWith("/rest/api/3/issue/ABC-1/worklog"));
    }

    [Fact]
    public async Task Editing_a_synced_entry_updates_its_worklog()
    {
        var entry = Entry();
        await _sync.SaveAsync(Server, entry);
        var worklogId = (await Stored(entry.Id)).JiraWorklogId!;

        var edited = await Stored(entry.Id);
        edited.End = new TimeOnly(11, 0);
        edited.Description = "More things";
        await _sync.SaveAsync(Server, edited);

        var stored = await Stored(entry.Id);
        Assert.Equal(worklogId, stored.JiraWorklogId);
        Assert.True(stored.IsSynced);
        var log = _jira.Worklogs["ABC-1"][worklogId];
        Assert.Equal(7200, (int)log["timeSpentSeconds"]!);
        Assert.Equal("More things", (string)log["comment"]!);
        Assert.Single(_jira.Worklogs["ABC-1"]);
    }

    [Fact]
    public async Task Moving_an_entry_to_another_issue_moves_the_worklog()
    {
        var entry = Entry();
        await _sync.SaveAsync(Server, entry);

        var moved = await Stored(entry.Id);
        moved.IssueKey = "ABC-2";
        await _sync.SaveAsync(Server, moved);

        var stored = await Stored(entry.Id);
        Assert.Empty(_jira.Worklogs["ABC-1"]);
        Assert.Single(_jira.Worklogs["ABC-2"]);
        Assert.Equal("ABC-2", stored.JiraWorklogIssueKey);
        Assert.True(stored.IsSynced);
    }

    [Fact]
    public async Task Deleting_a_synced_entry_deletes_its_worklog()
    {
        var entry = Entry();
        await _sync.SaveAsync(Server, entry);

        var error = await _sync.DeleteAsync(Server, await Stored(entry.Id));

        Assert.Null(error);
        Assert.Empty(_jira.Worklogs["ABC-1"]);
        Assert.Null(await _entries.GetAsync(entry.Id));
    }

    [Fact]
    public async Task A_failed_delete_in_Jira_keeps_the_entry()
    {
        var entry = Entry();
        await _sync.SaveAsync(Server, entry);
        _jira.FailWith = HttpStatusCode.Forbidden;

        var error = await _sync.DeleteAsync(Server, await Stored(entry.Id));

        Assert.NotNull(error);
        Assert.NotNull(await _entries.GetAsync(entry.Id));
    }

    [Fact]
    public async Task Deleting_an_entry_never_synced_does_not_call_Jira()
    {
        _jira.Offline = true;
        var entry = Entry();
        await _sync.SaveAsync(Server, entry);
        _jira.Requests.Clear();

        Assert.Null(await _sync.DeleteAsync(Server, await Stored(entry.Id)));
        Assert.Empty(_jira.Requests);
    }

    [Fact]
    public async Task When_Jira_is_down_the_entry_is_saved_as_not_synced_and_can_be_retried()
    {
        _jira.Offline = true;
        var entry = Entry();

        var result = await _sync.SaveAsync(Server, entry);

        Assert.True(result.Saved);
        Assert.NotNull(result.SyncError);
        var stored = await Stored(entry.Id);
        Assert.False(stored.IsSynced);
        Assert.Contains("Could not reach", stored.SyncError);

        _jira.Offline = false;
        Assert.Null(await _sync.SyncAsync(Server, stored));

        stored = await Stored(entry.Id);
        Assert.True(stored.IsSynced);
        Assert.Null(stored.SyncError);
        Assert.Single(_jira.Worklogs["ABC-1"]);
    }

    [Fact]
    public async Task A_worklog_deleted_in_Jira_is_created_again_on_the_next_save()
    {
        var entry = Entry();
        await _sync.SaveAsync(Server, entry);
        _jira.Worklogs["ABC-1"].Clear();

        var edited = await Stored(entry.Id);
        edited.Description = "again";
        await _sync.SaveAsync(Server, edited);

        Assert.Single(_jira.Worklogs["ABC-1"]);
        Assert.True((await Stored(entry.Id)).IsSynced);
    }

    [Fact]
    public async Task Validation_errors_save_nothing_and_send_nothing()
    {
        var result = await _sync.SaveAsync(Server, Entry(start: "10:00", end: "09:00"));

        Assert.False(result.Saved);
        Assert.Empty(_jira.Requests);
    }

    [Fact]
    public async Task SyncRange_retries_every_unsynced_entry_in_the_range()
    {
        _jira.Offline = true;
        await _sync.SaveAsync(Server, Entry("ABC-1", "09:00", "10:00"));
        await _sync.SaveAsync(Server, Entry("ABC-2", "10:00", "11:00"));
        _jira.Offline = false;

        var stillFailing = await _sync.SyncRangeAsync(Server, Day, Day);

        Assert.Equal(0, stillFailing);
        Assert.Empty(await _entries.GetUnsyncedAsync(Day, Day));
    }

    [Fact]
    public void Started_uses_jira_format_with_offset_without_colon()
    {
        var cet = TimeZoneInfo.CreateCustomTimeZone("test", TimeSpan.FromHours(2), "test", "test");

        Assert.Equal("2026-10-19T09:05:00.000+0200", JiraClient.FormatStarted(Day, new TimeOnly(9, 5), cet));
        Assert.Equal("2026-10-19T09:05:00.000-0330",
            JiraClient.FormatStarted(Day, new TimeOnly(9, 5), TimeZoneInfo.CreateCustomTimeZone("nf", TimeSpan.FromMinutes(-210), "nf", "nf")));
    }
}
