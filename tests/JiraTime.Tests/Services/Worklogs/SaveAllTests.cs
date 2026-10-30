using System.Net;
using JiraTime.Domain;
using static JiraTime.Tests.Support.TestEntries;
using JiraTime.Services.Worklogs;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services.Worklogs;

/// <summary>
/// Save all (<see cref="WorklogSync.SaveDaysAsync"/>): every unsaved day of a period at once, each checked against Jira
/// before anything is written. Saving one day is in WorklogSyncTests.
/// </summary>
public class SaveAllTests : IDisposable
{
    private static readonly DateOnly Monday = new(2026, 10, 19), Tuesday = new(2026, 10, 20), Wednesday = new(2026, 10, 21);

    private readonly TimeHarness _h = new();
    public void Dispose() => _h.Dispose();

    private async Task<PeriodSheet> SheetAsync()
    {
        var sheet = new PeriodSheet(Period.For(Monday), () => _h.Today);
        var (loader, statuses) = _h.PeriodLoading();
        await loader.LoadAsync(sheet, statuses);
        return sheet;
    }

    /// <summary>Adds a row on an issue with hours typed as <paramref name="hours"/>.</summary>
    private static void Add(PeriodSheet sheet, DateOnly day, string key, string hours) =>
        sheet.Day(day).SetHours(sheet.Day(day).AddRow(new JiraIssue(key, $"Summary of {key}")), hours);

    [Fact]
    public async Task Every_unsaved_day_is_saved_and_the_rest_are_reported()
    {
        var id = _h.Jira.AddWorklog("ABC-9", "2026-10-21T09:00:00.000+0200", 3600);
        var sheet = await SheetAsync();
        Add(sheet, Monday, "ABC-1", "8");
        Add(sheet, Tuesday, "ABC-2", "4");
        Add(sheet, Tuesday, "ABC-3", "lots");
        sheet.Day(Wednesday).SetHours(sheet.SavedOn(Wednesday)[0], "2");
        _h.Jira.Worklogs["ABC-9"][id]!["timeSpentSeconds"] = 3 * 3600; // changed in Jira meanwhile

        var result = await _h.Sync.SaveDaysAsync(sheet.PlanSaveAll());

        Assert.Equal([Monday], result.Saved.Keys);
        Assert.Equal([Tuesday], result.NeedsWork);
        Assert.Equal([Wednesday], result.ChangedInJira);
        Assert.Equal(Wednesday, result.FirstToFix); // the day the notice is about
        Assert.StartsWith("1 day not saved because the worklogs were changed in Jira", result.Notice);
        Assert.Equal(8 * 3600, _h.JiraSeconds("ABC-1"));
        Assert.Equal(0, _h.JiraSeconds("ABC-2"));
    }

    [Fact]
    public async Task One_search_checks_the_worklogs_of_every_day_before_any_is_written()
    {
        foreach (var day in new[] { "19", "20", "21" })
        {
            _h.Jira.AddWorklog("ABC-1", $"2026-10-{day}T09:00:00.000+0200", 3600);
            _h.Jira.AddWorklog("ABC-2", $"2026-10-{day}T09:00:00.000+0200", 3600);
        }
        var sheet = await SheetAsync();
        foreach (var day in new[] { Monday, Tuesday, Wednesday })
            foreach (var row in sheet.SavedOn(day))
                sheet.Day(day).SetComment(row, "Edited");
        _h.Jira.Requests.Clear();

        var result = await _h.Sync.SaveDaysAsync(sheet.PlanSaveAll());

        Assert.Equal([Monday, Tuesday, Wednesday], result.Saved.Keys.Order());
        var check = Assert.Single(_h.Jira.Requests.TakeWhile(r => r.Method != HttpMethod.Put));
        Assert.StartsWith("key in (", FakeJira.Jql(check));
        Assert.Equal(6, _h.Jira.Writes.Count());
        Assert.Single(_h.Jira.Searches, r => FakeJira.Jql(r).StartsWith("key in")); // not one per day
    }

    [Fact]
    public async Task When_every_day_is_saved_there_is_nothing_to_fix()
    {
        var sheet = await SheetAsync();
        Add(sheet, Monday, "ABC-1", "8");

        var result = await _h.Sync.SaveDaysAsync(sheet.PlanSaveAll());

        Assert.Null(result.FirstToFix);
        Assert.Null(result.Notice);
    }

    [Fact]
    public async Task A_Jira_comment_with_spaces_around_it_is_read_cleaned_and_not_sent_again()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 8 * 3600, "  Pairing  ");
        var sheet = await SheetAsync();
        var row = Assert.Single(sheet.SavedOn(Monday));
        Assert.Equal("Pairing", row.Comment);
        sheet.Day(Monday).SetComment(row, "Pairing "); // typed again as it was
        _h.Jira.Requests.Clear();

        await _h.Sync.SaveDaysAsync(sheet.PlanSaveAll());

        Assert.False(sheet.IsUnsaved(Monday));
        Assert.Empty(_h.Jira.Writes);
    }

    [Fact]
    public async Task Changing_the_hours_of_a_worklog_with_spaces_around_its_comment_sends_the_cleaned_comment()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 8 * 3600, "  Pairing  ");
        var sheet = await SheetAsync();
        sheet.Day(Monday).SetHours(sheet.SavedOn(Monday)[0], "6");

        var result = await _h.Sync.SaveDaysAsync(sheet.PlanSaveAll());

        Assert.Equal([Monday], result.Saved.Keys);
        Assert.Equal("Pairing", (string?)_h.Jira.Worklogs["ABC-1"].Values.Single()["comment"]);
        Assert.Equal(6 * 3600, _h.JiraSeconds("ABC-1"));
    }

    [Fact]
    public async Task A_day_taken_off_deletes_its_worklogs_in_Jira_on_save()
    {
        await _h.SaveDayAsync(Monday, [Row("ABC-1", 6), Row("ABC-2", 2)]);
        var sheet = await SheetAsync();
        await sheet.MarkDayOffAsync(Monday, wholeDay: true, () => Task.CompletedTask);

        var result = await _h.Sync.SaveDaysAsync(sheet.PlanSaveAll());

        Assert.Equal([Monday], result.Saved.Keys);
        Assert.Equal(0, _h.JiraSeconds("ABC-1") + _h.JiraSeconds("ABC-2"));
        Assert.Empty(await _h.DayAsync(Monday));
    }

    [Fact]
    public async Task A_worklog_changed_in_Jira_since_the_day_was_read_is_not_overwritten()
    {
        var id = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var saved = await _h.DayAsync(Monday);
        var rows = saved.Select(e => e.Copy()).ToList();
        rows[0].Duration = TimeSpan.FromHours(2);
        _h.Jira.Worklogs["ABC-1"][id]!["timeSpentSeconds"] = 3 * 3600; // someone edits it in Jira meanwhile

        var result = await _h.SaveDayAsync(Monday, rows, saved);

        Assert.True(result.ChangedInJira);
        Assert.False(result.Saved);
        Assert.Equal(WorklogSync.ChangedInJiraMessage, result.Errors[DaySave.DayError]);
        Assert.Equal(3 * 3600, _h.JiraSeconds("ABC-1"));
        Assert.Equal(TimeSpan.FromHours(3), Assert.Single(await _h.DayAsync(Monday)).Duration); // read again
    }

    [Fact]
    public async Task A_worklog_deleted_in_Jira_since_the_day_was_read_stops_the_save()
    {
        var id = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var saved = await _h.DayAsync(Monday);
        var rows = saved.Select(e => e.Copy()).Append(Row("ABC-2", 1)).ToList();
        rows[0].Comment = "Edited";
        _h.Jira.Worklogs["ABC-1"].Remove(id);

        var result = await _h.SaveDayAsync(Monday, rows, saved);

        Assert.True(result.ChangedInJira);
        Assert.Equal(0, _h.JiraSeconds("ABC-2")); // nothing written, the new row neither
    }

    [Fact]
    public async Task The_check_before_saving_reads_every_issue_in_one_search()
    {
        foreach (var key in new[] { "ABC-1", "ABC-2", "ABC-3" })
        {
            _h.Jira.AddWorklog(key, "2026-10-19T09:00:00.000+0200", 3600);
            _h.Jira.AddWorklog(key, "2026-10-19T09:00:00.000+0200", 1800);
        }
        var rows = await _h.CopiesAsync(Monday);
        foreach (var row in rows) row.Comment = "Edited";
        _h.Jira.Requests.Clear();

        Assert.True((await _h.SaveDayAsync(Monday, rows)).Saved);

        var check = _h.Jira.Requests.TakeWhile(r => r.Method != HttpMethod.Put).ToList();
        var search = Assert.Single(check);
        Assert.StartsWith("key in (", FakeJira.Jql(search));
        Assert.Contains("validateQuery=warn", search.Path);
        Assert.Equal(6, _h.Jira.Writes.Count());
    }

    [Fact]
    public async Task An_issue_with_more_worklogs_than_the_search_sends_is_read_in_full_for_the_check()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var later = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 1800);
        var saved = await _h.DayAsync(Monday);
        var rows = saved.Select(e => e.Copy()).ToList();
        rows.Single(r => r.JiraWorklogId == later).Comment = "Edited";
        _h.Jira.Worklogs["ABC-1"][later]!["timeSpentSeconds"] = 7200; // changed in Jira, beyond what the search sends
        _h.Jira.EmbeddedWorklogLimit = 1;
        _h.Jira.Requests.Clear();

        Assert.True((await _h.SaveDayAsync(Monday, rows, saved)).ChangedInJira);
        Assert.StartsWith("/rest/api/2/issue/ABC-1/worklog?", Assert.Single(_h.Jira.WorklogReads).Path);
    }

    [Fact]
    public async Task An_issue_deleted_in_Jira_since_the_day_was_read_stops_the_save()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        _h.Jira.AddWorklog("ABC-2", "2026-10-19T09:00:00.000+0200", 3600);
        var saved = await _h.DayAsync(Monday);
        var rows = saved.Select(e => e.Copy()).ToList();
        foreach (var row in rows) row.Comment = "Edited";
        _h.Jira.Worklogs.Remove("ABC-2");
        _h.Jira.Requests.Clear();

        Assert.True((await _h.SaveDayAsync(Monday, rows, saved)).ChangedInJira);
        Assert.Empty(_h.Jira.Writes);
    }

    [Fact]
    public async Task When_Jira_cannot_be_asked_before_saving_the_save_goes_ahead()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var rows = await _h.CopiesAsync(Monday);
        rows[0].Comment = "Edited";
        _h.Jira.FailWhen = uri => uri.AbsolutePath.EndsWith("/search") ? HttpStatusCode.ServiceUnavailable : null;

        Assert.True((await _h.SaveDayAsync(Monday, rows)).Saved);
        Assert.Equal("Edited", (string?)_h.Jira.Worklogs["ABC-1"].Values.Single()["comment"]);
    }

    [Fact]
    public async Task Rows_only_added_need_no_check_against_Jira()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        var rows = (await _h.CopiesAsync(Monday)).Append(Row("ABC-2", 1)).ToList();
        _h.Jira.Requests.Clear();

        Assert.True((await _h.SaveDayAsync(Monday, rows)).Saved);

        Assert.DoesNotContain(_h.Jira.Searches, r => FakeJira.Jql(r).StartsWith("key in"));
    }
}
