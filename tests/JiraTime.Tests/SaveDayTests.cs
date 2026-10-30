using System.Net;
using JiraTime.Domain;
using JiraTime.Services;
using static JiraTime.Tests.TimeHarness;

namespace JiraTime.Tests;

/// <summary>Saving a day writes its worklogs straight to Jira; failed writes wait in the retry queue.</summary>
public class SaveDayTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TimeHarness _h = new();
    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task A_new_day_creates_every_row_in_Jira_and_stores_nothing_locally()
    {
        var result = await _h.SaveDayAsync(Day, [Row("ABC-1", 6), Row("ABC-2", 2, "Review")]);

        Assert.True(result.Saved);
        Assert.Equal(0, result.SyncFailures);
        Assert.Equal(6 * 3600, _h.JiraSeconds("ABC-1"));
        Assert.Equal(2 * 3600, _h.JiraSeconds("ABC-2"));
        Assert.Empty(await _h.QueuedAsync());
        var day = await _h.DayAsync(Day);
        Assert.Equal(["ABC-1", "ABC-2"], day.Select(e => e.IssueKey).Order());
        Assert.All(day, e => Assert.True(e.IsSynced));
    }

    [Fact]
    public async Task Saved_rows_are_stamped_with_the_clock()
    {
        var row = Row("ABC-1", 8);

        await _h.SaveDayAsync(Day, [row]);

        Assert.Equal(_h.Clock.Now.UtcDateTime, row.UpdatedUtc);
    }

    [Fact]
    public async Task Saving_again_updates_changed_rows_removes_missing_ones_and_leaves_the_rest()
    {
        await _h.SaveDayAsync(Day, [Row("ABC-1", 4), Row("ABC-2", 2), Row("ABC-3", 2)]);
        var rows = await _h.CopiesAsync(Day);
        rows.Single(r => r.IssueKey == "ABC-1").Duration = TimeSpan.FromHours(5);
        rows.RemoveAll(r => r.IssueKey == "ABC-3");
        rows.Add(Row("ABC-4", 1));
        _h.Jira.Requests.Clear();

        var result = await _h.SaveDayAsync(Day, rows);
        var sent = _h.Jira.Requests.ToList();

        Assert.True(result.Saved);
        Assert.DoesNotContain(sent, r => r.Path.Contains("ABC-2")); // unchanged: not sent, not checked
        Assert.Equal(["ABC-1", "ABC-2", "ABC-4"], (await _h.DayAsync(Day)).Select(e => e.IssueKey).Order());
        Assert.Equal(5 * 3600, _h.JiraSeconds("ABC-1"));
        Assert.Equal(0, _h.JiraSeconds("ABC-3"));
        Assert.Equal(3600, _h.JiraSeconds("ABC-4"));
        Assert.Single(_h.Jira.Worklogs["ABC-1"]);                                // updated, not duplicated
    }

    [Fact]
    public async Task Worklogs_logged_elsewhere_are_edited_and_removed_like_any_other()
    {
        _h.Jira.AddWorklog("OPS-9", "2026-10-19T13:00:00.000+0200", 7200, "Support");
        _h.Jira.AddWorklog("OPS-8", "2026-10-19T13:00:00.000+0200", 3600);
        var rows = await _h.CopiesAsync(Day);
        rows.Single(r => r.IssueKey == "OPS-9").Description = "Support call";
        rows.RemoveAll(r => r.IssueKey == "OPS-8");

        Assert.True((await _h.SaveDayAsync(Day, rows)).Saved);

        Assert.Equal("Support call", (string?)_h.Jira.Worklogs["OPS-9"].Values.Single()["comment"]);
        Assert.Equal(0, _h.JiraSeconds("OPS-8"));
    }

    [Fact]
    public async Task Moving_a_row_to_another_issue_moves_its_worklog()
    {
        await _h.SaveDayAsync(Day, [Row("ABC-1", 2)]);
        var rows = await _h.CopiesAsync(Day);
        rows[0].IssueKey = "ABC-2";

        Assert.True((await _h.SaveDayAsync(Day, rows)).Saved);

        Assert.Equal(0, _h.JiraSeconds("ABC-1"));
        Assert.Equal(2 * 3600, _h.JiraSeconds("ABC-2"));
        Assert.Equal("ABC-2", Assert.Single(await _h.DayAsync(Day)).IssueKey);
    }

    [Fact]
    public async Task One_invalid_row_saves_nothing()
    {
        var result = await _h.SaveDayAsync(Day, [Row("ABC-1", 2), Row("", 1)]);

        Assert.False(result.Saved);
        Assert.Single(result.Errors);
        Assert.Empty(_h.Jira.Worklogs);
    }

    [Fact]
    public async Task More_than_24_hours_is_a_day_error()
    {
        var result = await _h.SaveDayAsync(Day, [Row("ABC-1", 20), Row("ABC-2", 5)]);

        Assert.Equal(TimeEntry.TooManyHours, result.Errors[WorklogSync.DayResult.DayError]);
        Assert.Empty(_h.Jira.Worklogs);
    }

    [Fact]
    public async Task Moving_hours_between_rows_near_24_hours_is_allowed()
    {
        await _h.SaveDayAsync(Day, [Row("ABC-1", 20), Row("ABC-2", 4)]);
        var rows = await _h.CopiesAsync(Day);
        rows.Single(r => r.IssueKey == "ABC-1").Duration = TimeSpan.FromHours(16);
        rows.Single(r => r.IssueKey == "ABC-2").Duration = TimeSpan.FromHours(8);

        Assert.True((await _h.SaveDayAsync(Day, rows)).Saved);
    }

    [Fact]
    public async Task A_worklog_changed_in_Jira_since_the_day_was_read_is_not_overwritten()
    {
        var id = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var saved = await _h.DayAsync(Day);
        var rows = saved.Select(e => e.Copy()).ToList();
        rows[0].Duration = TimeSpan.FromHours(2);
        _h.Jira.Worklogs["ABC-1"][id]!["timeSpentSeconds"] = 3 * 3600; // someone edits it in Jira meanwhile

        var result = await _h.SaveDayAsync(Day, rows, saved);

        Assert.True(result.ChangedInJira);
        Assert.False(result.Saved);
        Assert.Equal(WorklogSync.ChangedInJiraMessage, result.Errors[WorklogSync.DayResult.DayError]);
        Assert.Equal(3 * 3600, _h.JiraSeconds("ABC-1"));
        Assert.Equal(TimeSpan.FromHours(3), Assert.Single(await _h.DayAsync(Day)).Duration); // read again
    }

    [Fact]
    public async Task A_worklog_deleted_in_Jira_since_the_day_was_read_stops_the_save()
    {
        var id = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var saved = await _h.DayAsync(Day);
        var rows = saved.Select(e => e.Copy()).Append(Row("ABC-2", 1)).ToList();
        rows[0].Description = "Edited";
        _h.Jira.Worklogs["ABC-1"].Remove(id);

        var result = await _h.SaveDayAsync(Day, rows, saved);

        Assert.True(result.ChangedInJira);
        Assert.Equal(0, _h.JiraSeconds("ABC-2")); // nothing written, the new row neither
    }

    [Fact]
    public async Task Rows_only_added_need_no_check_against_Jira()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var rows = (await _h.CopiesAsync(Day)).Append(Row("ABC-2", 1)).ToList();
        _h.Jira.Requests.Clear();

        Assert.True((await _h.SaveDayAsync(Day, rows)).Saved);

        Assert.DoesNotContain(_h.Jira.Requests, r => r.Path.Contains("issue/ABC-1/worklog/"));
    }

    [Fact]
    public async Task Worklog_ids_come_from_what_was_read_not_from_the_edited_row()
    {
        await _h.SaveDayAsync(Day, [Row("ABC-1", 2)]);
        var rows = await _h.CopiesAsync(Day);
        rows[0].Duration = TimeSpan.FromHours(3);
        rows[0].JiraWorklogId = "99999"; // a stray value on the copy

        Assert.True((await _h.SaveDayAsync(Day, rows)).Saved);

        Assert.Single(_h.Jira.Worklogs["ABC-1"]);
        Assert.Equal(3 * 3600, _h.JiraSeconds("ABC-1"));
    }

    [Fact]
    public async Task Cloud_uses_api_v3_and_a_document_comment()
    {
        using var cloud = new TimeHarness(Cloud);

        await cloud.SaveDayAsync(Day, [Row("ABC-1", 1, "Review")]);

        var post = Assert.Single(cloud.Jira.Requests, r => r.Method == HttpMethod.Post);
        Assert.StartsWith("/rest/api/3/issue/ABC-1/worklog", post.Path);
        Assert.Equal("doc", (string?)post.Body!["comment"]!["type"]);
    }

    [Fact]
    public void Started_uses_jira_format_with_offset_without_colon()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Plus2", TimeSpan.FromHours(2), "Plus2", "Plus2");
        Assert.Equal("2026-10-19T09:00:00.000+0200", JiraWorklogClient.FormatStarted(Day, new TimeOnly(9, 0), zone));
    }

    [Fact]
    public async Task A_day_taken_off_deletes_its_worklogs_in_Jira_on_save()
    {
        await _h.SaveDayAsync(Day, [Row("ABC-1", 6), Row("ABC-2", 2)]);
        var saved = await _h.DayAsync(Day);
        var work = new DayWork(Day);

        work.TakeOff(saved);
        var result = await _h.SaveDayAsync(Day, work.RowsToSave(saved), saved);

        Assert.True(result.Saved);
        Assert.Equal(0, _h.JiraSeconds("ABC-1") + _h.JiraSeconds("ABC-2"));
        Assert.Empty(await _h.DayAsync(Day));
    }
}
