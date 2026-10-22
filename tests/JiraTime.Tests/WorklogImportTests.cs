using System.Text.Json;
using System.Text.Json.Nodes;
using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage.FileSystem;

namespace JiraTime.Tests;

public class WorklogImportTests : IDisposable
{
    private static readonly DateOnly From = new(2026, 10, 16), To = new(2026, 10, 31);
    private static readonly DateOnly Day = new(2026, 10, 19);
    private static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token", null);
    private static readonly JiraProfile Me = new("JIRAUSER1", "carl", "Carl", null);

    private readonly TempDir _dir = new();
    private readonly FileDocumentStore _store;
    private readonly TimeEntryService _entries;
    private readonly FakeJira _jira = new();
    private readonly WorklogSync _sync;

    public WorklogImportTests()
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

    private Task<WorklogSync.ImportResult> Import() => _sync.ImportAsync(Server, Me, From, To);
    private Task<IReadOnlyList<TimeEntry>> Period() => _entries.GetRangeAsync(From, To);

    [Fact]
    public async Task Worklogs_logged_in_Jira_are_imported_and_linked_by_id()
    {
        var id = _jira.AddWorklog("OPS-9", "2026-10-19T13:00:00.000+0200", 7200, "Support");

        var result = await Import();

        Assert.Equal(1, result.Added);
        var e = Assert.Single(await Period());
        Assert.Equal(("OPS-9", "Summary of OPS-9", Day, TimeSpan.FromHours(2), "Support"), (e.IssueKey, e.IssueSummary, e.Date, e.Duration, e.Description));
        Assert.Equal(id, e.JiraWorklogId);
        Assert.True(e.IsSynced);
        Assert.True(e.FromJira);
    }

    [Fact]
    public async Task Importing_again_adds_nothing()
    {
        _jira.AddWorklog("OPS-9", "2026-10-19T13:00:00.000+0200", 7200);
        await Import();

        var again = await Import();

        Assert.Equal(0, again.Changes);
        Assert.Single(await Period());
    }

    [Fact]
    public async Task Work_saved_here_is_not_imported_twice()
    {
        await _sync.SaveDayAsync(Server, Day, [new TimeEntry { IssueKey = "ABC-1", Duration = TimeSpan.FromHours(6) }]);

        var result = await Import();

        Assert.Equal(0, result.Changes);
        var e = Assert.Single(await Period());
        Assert.False(e.FromJira);
    }

    [Fact]
    public async Task Other_peoples_worklogs_and_days_outside_the_range_are_skipped()
    {
        _jira.AddWorklog("OPS-9", "2026-10-19T13:00:00.000+0200", 3600, author: new JsonObject { ["key"] = "JIRAUSER2", ["name"] = "anna" });
        _jira.AddWorklog("OPS-9", "2026-10-15T13:00:00.000+0200", 3600);
        _jira.AddWorklog("OPS-9", "2026-11-01T13:00:00.000+0100", 3600);

        Assert.Equal(0, (await Import()).Added);
        Assert.Empty(await Period());
    }

    [Fact]
    public async Task Changes_made_in_Jira_win_for_synced_work()
    {
        await _sync.SaveDayAsync(Server, Day, [new TimeEntry { IssueKey = "ABC-1", Duration = TimeSpan.FromHours(6) }]);
        var log = _jira.Worklogs["ABC-1"].Single();
        log.Value["timeSpentSeconds"] = 5 * 3600;
        log.Value["comment"] = "Edited in Jira";
        log.Value["started"] = "2026-10-20T09:00:00.000+0200";

        var result = await Import();

        Assert.Equal(1, result.Updated);
        var e = Assert.Single(await Period());
        Assert.Equal((new DateOnly(2026, 10, 20), TimeSpan.FromHours(5), "Edited in Jira"), (e.Date, e.Duration, e.Description));
        Assert.True(e.IsSynced);
    }

    [Fact]
    public async Task Local_changes_not_yet_in_Jira_are_kept()
    {
        _jira.AddWorklog("OPS-9", "2026-10-19T13:00:00.000+0200", 7200);
        await Import();
        var e = (await Period()).Single();
        e.Duration = TimeSpan.FromHours(3);
        await _entries.SaveAsync(e); // pending, not sent

        await Import();

        Assert.Equal(TimeSpan.FromHours(3), (await Period()).Single().Duration);
    }

    [Fact]
    public async Task Work_deleted_in_Jira_is_removed_here()
    {
        await _sync.SaveDayAsync(Server, Day, [new TimeEntry { IssueKey = "ABC-1", Duration = TimeSpan.FromHours(6) }]);
        _jira.Worklogs["ABC-1"].Clear();

        var result = await Import();

        Assert.Equal(1, result.Removed);
        Assert.Empty(await Period());
    }

    [Fact]
    public async Task Work_missing_from_a_lagging_search_index_is_kept()
    {
        await _sync.SaveDayAsync(Server, Day, [new TimeEntry { IssueKey = "ABC-1", Duration = TimeSpan.FromHours(6) }]);
        _jira.NotIndexed.Add("ABC-1");

        var result = await Import();

        Assert.Equal(0, result.Changes);
        Assert.Single(await Period());
    }

    [Fact]
    public async Task Unsynced_work_is_never_removed_by_an_import()
    {
        _jira.Offline = true;
        await _sync.SaveDayAsync(Server, Day, [new TimeEntry { IssueKey = "ABC-1", Duration = TimeSpan.FromHours(6) }]);
        _jira.Offline = false;

        await Import();

        Assert.Single(await Period());
    }

    [Theory]
    [InlineData("\"Plain text\"", "Plain text")]
    [InlineData("""{"type":"doc","version":1,"content":[{"type":"paragraph","content":[{"type":"text","text":"Line "},{"type":"text","text":"one"}]},{"type":"paragraph","content":[{"type":"text","text":"Two"}]}]}""", "Line one\nTwo")]
    [InlineData("""{"type":"doc","version":1,"content":[]}""", null)]
    [InlineData("\"  \"", null)]
    public void Comments_are_read_from_text_and_document_format(string json, string? expected) =>
        Assert.Equal(expected, JiraClient.CommentText(JsonDocument.Parse(json).RootElement));

    [Theory]
    [InlineData(3600, 60)]
    [InlineData(5400, 90)]
    [InlineData(89, 1)]
    [InlineData(91, 2)]
    [InlineData(10, 1)]
    public void Imported_time_is_in_whole_minutes(int seconds, int minutes) =>
        Assert.Equal(TimeSpan.FromMinutes(minutes), JiraClient.WholeMinutes(seconds));
}
