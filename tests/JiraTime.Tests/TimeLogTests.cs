using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>Reading logged time: Jira's worklogs, with work waiting for Jira laid over them.</summary>
public class TimeLogTests : IDisposable
{
    private static readonly DateOnly From = new(2026, 10, 16), To = new(2026, 10, 31);
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TimeHarness _h = new();
    public void Dispose() => _h.Dispose();

    private Task<LoggedTime> Period() => _h.Log.GetRangeAsync(From, To);

    [Fact]
    public async Task Worklogs_however_they_were_logged_are_the_time()
    {
        var id = _h.Jira.AddWorklog("OPS-9", "2026-10-19T13:00:00.000+0200", 7200, "Support");

        var time = await Period();

        Assert.True(time.IsComplete);
        var e = Assert.Single(time.Entries);
        Assert.Equal(("OPS-9", "Summary of OPS-9", Day, TimeSpan.FromHours(2), "Support"), (e.IssueKey, e.IssueSummary, e.Date, e.Duration, e.Description));
        Assert.Equal((id, "jira-" + id), (e.JiraWorklogId, e.Id));
        Assert.True(e.IsSynced);
        Assert.Empty(await _h.QueuedAsync()); // nothing is stored locally
    }

    [Fact]
    public async Task Other_peoples_worklogs_and_days_outside_the_range_are_left_out()
    {
        _h.Jira.AddWorklog("OPS-9", "2026-10-19T09:00:00.000+0200", 3600, author: new JsonObject { ["key"] = "JIRAUSER2", ["name"] = "anna" });
        _h.Jira.AddWorklog("OPS-9", "2026-11-02T09:00:00.000+0100", 3600);
        _h.Jira.AddWorklog("OPS-9", "2026-10-20T09:00:00.000+0200", 1800);

        var e = Assert.Single((await Period()).Entries);
        Assert.Equal(new DateOnly(2026, 10, 20), e.Date);
    }

    [Fact]
    public async Task A_day_is_in_the_order_its_work_was_logged()
    {
        var later = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var earlier = _h.Jira.AddWorklog("ABC-2", "2026-10-19T09:00:00.000+0200", 3600);
        _h.Jira.Worklogs["ABC-1"][later]!["created"] = "2026-10-19T15:00:00.000+0200";
        _h.Jira.Worklogs["ABC-2"][earlier]!["created"] = "2026-10-19T10:00:00.000+0200";

        Assert.Equal(["ABC-2", "ABC-1"], (await Period()).Entries.Select(e => e.IssueKey));
    }

    [Fact]
    public async Task Work_just_saved_shows_while_Jiras_search_has_not_caught_up()
    {
        var result = await _h.SaveDayAsync(Day, [TimeHarness.Row("NEW-1", 3)]);
        Assert.True(result.Saved);
        _h.Jira.NotIndexed.Add("NEW-1"); // the search doesn't find the issue yet

        Assert.Equal("NEW-1", Assert.Single((await Period()).Entries).IssueKey);
    }

    [Fact]
    public async Task Work_just_removed_is_gone_even_while_the_search_still_finds_it()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        await _h.SaveDayAsync(Day, []);

        Assert.Empty((await Period()).Entries); // read from the issue itself, which is up to date
    }

    [Fact]
    public async Task Issues_are_read_directly_only_for_a_while_after_a_save()
    {
        await _h.SaveDayAsync(Day, [TimeHarness.Row("NEW-1", 3)]);
        _h.Jira.NotIndexed.Add("NEW-1");
        _h.Clock.Advance(RecentWorklogIssues.Window + TimeSpan.FromMinutes(1));
        _h.Log.Refresh();

        Assert.Empty((await Period()).Entries); // by now Jira's search is trusted again
    }

    [Fact]
    public async Task Waiting_changes_replace_their_worklog_and_waiting_removals_hide_it()
    {
        var edited = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var removed = _h.Jira.AddWorklog("ABC-2", "2026-10-19T09:00:00.000+0200", 3600);
        await _h.Queue.AddAsync(new TimeEntry
        {
            IssueKey = "ABC-1", Date = Day, Duration = TimeSpan.FromHours(4), JiraWorklogId = edited, JiraWorklogIssueKey = "ABC-1", SyncPending = true,
        });
        await _h.Queue.AddAsync(new TimeEntry
        {
            IssueKey = "ABC-2", Date = Day, Duration = TimeSpan.FromHours(1), JiraWorklogId = removed, JiraWorklogIssueKey = "ABC-2",
            SyncPending = true, DeletePending = true,
        });
        await _h.Queue.AddAsync(new TimeEntry { IssueKey = "ABC-3", Date = Day, Duration = TimeSpan.FromHours(2), SyncPending = true });

        var entries = (await Period()).Entries;

        Assert.Equal(["ABC-1 4 False", "ABC-3 2 False"], entries.Select(e => $"{e.IssueKey} {e.Duration.TotalHours} {e.IsSynced}").Order());
    }

    [Fact]
    public async Task When_Jira_cannot_be_read_only_work_waiting_for_Jira_shows_with_the_reason()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        await _h.Queue.AddAsync(new TimeEntry { IssueKey = "ABC-3", Date = Day, Duration = TimeSpan.FromHours(2), SyncPending = true });
        _h.Jira.FailWith = HttpStatusCode.ServiceUnavailable;

        var time = await Period();

        Assert.False(time.IsComplete);
        Assert.NotNull(time.JiraError);
        Assert.Equal("ABC-3", Assert.Single(time.Entries).IssueKey);
    }

    [Fact]
    public async Task Reads_are_reused_briefly_and_Refresh_reads_again()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        await Period();
        _h.Jira.AddWorklog("ABC-2", "2026-10-19T09:00:00.000+0200", 3600); // logged in Jira meanwhile

        Assert.Single((await Period()).Entries);
        _h.Log.Refresh();
        Assert.Equal(2, (await Period()).Entries.Count);
    }

    [Fact]
    public async Task Reads_expire_after_a_minute()
    {
        await Period();
        _h.Jira.AddWorklog("ABC-2", "2026-10-19T09:00:00.000+0200", 3600);
        _h.Clock.Advance(TimeLog.CacheLifetime);

        Assert.Single((await Period()).Entries);
    }

    [Fact]
    public async Task Recent_work_covers_the_last_60_days()
    {
        _h.Jira.AddWorklog("OLD-1", "2026-08-01T09:00:00.000+0200", 3600, "Too old");
        _h.Jira.AddWorklog("ABC-1", "2026-09-01T09:00:00.000+0200", 3600, "Planning");

        var recent = await _h.Log.GetRecentAsync();

        Assert.Equal(["ABC-1"], recent.Select(e => e.IssueKey));
    }

    [Theory]
    [InlineData("\"Plain text\"", "Plain text")]
    [InlineData("""{"type":"doc","version":1,"content":[{"type":"paragraph","content":[{"type":"text","text":"Line "},{"type":"text","text":"one"}]},{"type":"paragraph","content":[{"type":"text","text":"Two"}]}]}""", "Line one\nTwo")]
    [InlineData("""{"type":"doc","version":1,"content":[]}""", null)]
    [InlineData("\"  \"", null)]
    public void Comments_are_read_from_text_and_document_format(string json, string? expected) =>
        Assert.Equal(expected, AtlassianDocument.ToPlainText(JsonDocument.Parse(json).RootElement));

    [Theory]
    [InlineData(3600, 60)]
    [InlineData(5400, 90)]
    [InlineData(89, 1)]
    [InlineData(91, 2)]
    [InlineData(10, 1)]
    public void Worklog_time_is_read_in_whole_minutes(int seconds, int minutes) =>
        Assert.Equal(TimeSpan.FromMinutes(minutes), JiraClient.WholeMinutes(seconds));

    [Theory]
    [InlineData("2026-10-19T09:30:00.000+0200", "2026-10-19T07:30:00Z")]
    [InlineData("2026-10-19T09:30:00.000-0500", "2026-10-19T14:30:00Z")]
    [InlineData("2026-10-19T09:30:00.000+02:00", "2026-10-19T07:30:00Z")]
    [InlineData("nonsense", null)]
    [InlineData(null, null)]
    public void Created_times_are_read_with_or_without_a_colon_in_the_offset(string? text, string? utc) =>
        Assert.Equal(utc is null ? null : DateTimeOffset.Parse(utc), JiraClient.ParseCreated(text));
}
