using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage.FileSystem;

namespace JiraTime.Tests;

public class SaveDayTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);
    private static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token", null);

    private readonly TempDir _dir = new();
    private readonly FileDocumentStore _store;
    private readonly TimeEntryService _entries;
    private readonly FakeJira _jira = new();
    private readonly WorklogSync _sync;

    public SaveDayTests()
    {
        _store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = _dir.Path });
        _entries = new TimeEntryService(_store, TestUsers.SignedIn());
        _sync = new WorklogSync(new JiraClient(_jira.Client()), _entries);
    }

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }

    private static TimeEntry Row(string key, double hours, string? desc = null) =>
        new() { IssueKey = key, Duration = TimeSpan.FromHours(hours), Description = desc };

    private async Task<List<TimeEntry>> Saved() => (await _entries.GetRangeAsync(Day, Day)).ToList();

    private int JiraSeconds(string key) =>
        _jira.Worklogs.TryGetValue(key, out var logs) ? logs.Values.Sum(l => (int)l["timeSpentSeconds"]!) : 0;

    private List<TimeEntry> Copies(IEnumerable<TimeEntry> es) => es.Select(e => e.Copy()).ToList();

    [Fact]
    public async Task A_new_day_creates_every_row_in_Jira()
    {
        var result = await _sync.SaveDayAsync(Server, Day, [Row("ABC-1", 6), Row("ABC-2", 2, "Review")]);

        Assert.True(result.Saved);
        Assert.Equal(0, result.SyncFailures);
        var saved = await Saved();
        Assert.Equal(2, saved.Count);
        Assert.All(saved, e => { Assert.Equal(Day, e.Date); Assert.True(e.IsSynced); });
        Assert.Equal(6 * 3600, JiraSeconds("ABC-1"));
        Assert.Equal(2 * 3600, JiraSeconds("ABC-2"));
    }

    [Fact]
    public async Task Saving_again_updates_changed_rows_removes_missing_ones_and_leaves_the_rest()
    {
        await _sync.SaveDayAsync(Server, Day, [Row("ABC-1", 4), Row("ABC-2", 2), Row("ABC-3", 2)]);
        var rows = Copies(await Saved());
        var one = rows.Single(r => r.IssueKey == "ABC-1");
        one.Duration = TimeSpan.FromHours(5);
        rows.RemoveAll(r => r.IssueKey == "ABC-3");
        rows.Add(Row("ABC-4", 1));
        _jira.Requests.Clear();

        var result = await _sync.SaveDayAsync(Server, Day, rows);

        Assert.True(result.Saved);
        Assert.Equal(["ABC-1", "ABC-2", "ABC-4"], (await Saved()).Select(e => e.IssueKey).Order());
        Assert.Equal(5 * 3600, JiraSeconds("ABC-1"));
        Assert.Equal(0, JiraSeconds("ABC-3"));          // removed in Jira too
        Assert.Equal(3600, JiraSeconds("ABC-4"));
        Assert.DoesNotContain(_jira.Requests, r => r.Path.Contains("ABC-2")); // unchanged: not sent
        Assert.Single(_jira.Worklogs["ABC-1"]);          // updated, not duplicated
    }

    [Fact]
    public async Task An_empty_list_removes_the_whole_day()
    {
        await _sync.SaveDayAsync(Server, Day, [Row("ABC-1", 8)]);

        Assert.True((await _sync.SaveDayAsync(Server, Day, [])).Saved);

        Assert.Empty(await Saved());
        Assert.Equal(0, JiraSeconds("ABC-1"));
    }

    [Fact]
    public async Task One_invalid_row_saves_nothing()
    {
        await _sync.SaveDayAsync(Server, Day, [Row("ABC-1", 8)]);
        _jira.Requests.Clear();
        var bad = Row("", 2);

        var result = await _sync.SaveDayAsync(Server, Day, [Row("ABC-2", 6), bad]);

        Assert.False(result.Saved);
        Assert.Equal("Pick a Jira issue.", result.Errors[bad.Id]);
        Assert.Equal(["ABC-1"], (await Saved()).Select(e => e.IssueKey));
        Assert.Empty(_jira.Requests);
    }

    [Fact]
    public async Task More_than_24_hours_is_a_day_error()
    {
        var result = await _sync.SaveDayAsync(Server, Day, [Row("ABC-1", 20), Row("ABC-2", 5)]);

        Assert.Equal(TimeEntryService.TooManyHours, result.Errors[WorklogSync.DayResult.DayError]);
        Assert.Empty(await Saved());
    }

    [Fact]
    public async Task Moving_hours_between_rows_near_24_hours_is_allowed()
    {
        await _sync.SaveDayAsync(Server, Day, [Row("ABC-1", 20), Row("ABC-2", 4)]);
        var rows = Copies(await Saved());
        rows[0].Duration = TimeSpan.FromHours(4);
        rows[1].Duration = TimeSpan.FromHours(20);

        Assert.True((await _sync.SaveDayAsync(Server, Day, rows)).Saved);
    }

    [Fact]
    public async Task When_Jira_is_down_the_day_is_saved_and_failures_are_counted()
    {
        _jira.Offline = true;

        var result = await _sync.SaveDayAsync(Server, Day, [Row("ABC-1", 6), Row("ABC-2", 2)]);

        Assert.True(result.Saved);
        Assert.Equal(2, result.SyncFailures);
        Assert.All(await Saved(), e => Assert.False(e.IsSynced));

        _jira.Offline = false;
        var retry = await _sync.SaveDayAsync(Server, Day, Copies(await Saved())); // unchanged but unsynced rows are retried
        Assert.Equal(0, retry.SyncFailures);
        Assert.All(await Saved(), e => Assert.True(e.IsSynced));
    }

    [Fact]
    public async Task Worklog_ids_come_from_storage_not_from_the_edited_row()
    {
        await _sync.SaveDayAsync(Server, Day, [Row("ABC-1", 8)]);
        var row = Copies(await Saved())[0];
        var realId = row.JiraWorklogId;
        row.JiraWorklogId = null; // a stale copy must not create a second worklog
        row.Description = "Changed";

        await _sync.SaveDayAsync(Server, Day, [row]);

        Assert.Single(_jira.Worklogs["ABC-1"]);
        Assert.Equal(realId, (await Saved())[0].JiraWorklogId);
    }
}
