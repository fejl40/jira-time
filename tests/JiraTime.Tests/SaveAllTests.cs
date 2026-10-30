using JiraTime.Domain;
using static JiraTime.Tests.TimeHarness;

namespace JiraTime.Tests;

/// <summary>Save all: every unsaved day of a period at once.</summary>
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

    [Fact]
    public async Task Every_unsaved_day_is_saved_and_the_rest_are_reported()
    {
        var id = _h.Jira.AddWorklog("ABC-9", "2026-10-21T09:00:00.000+0200", 3600);
        var sheet = await SheetAsync();
        sheet.WorkFor(Monday).Drafts.Add(Row("ABC-1", 8));
        sheet.WorkFor(Tuesday).Drafts.Add(Row("ABC-2", 4));
        sheet.WorkFor(Tuesday).BadHours["x"] = "lots";
        sheet.WorkFor(Wednesday).Edit(sheet.SavedOn(Wednesday)[0]).Duration = TimeSpan.FromHours(2);
        _h.Jira.Worklogs["ABC-9"][id]!["timeSpentSeconds"] = 3 * 3600; // changed in Jira meanwhile

        var result = await _h.Sync.SaveDaysAsync(_h.Connection, sheet);

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
                sheet.WorkFor(day).Edit(row).Description = "Edited";
        _h.Jira.Requests.Clear();

        var result = await _h.Sync.SaveDaysAsync(_h.Connection, sheet);

        Assert.Equal([Monday, Tuesday, Wednesday], result.Saved.Keys.Order());
        var check = Assert.Single(_h.Jira.Requests.TakeWhile(r => r.Method != HttpMethod.Put));
        Assert.StartsWith("key in (", FakeJira.Jql(check));
        Assert.Equal(6, _h.Jira.Requests.Count(r => r.Method == HttpMethod.Put));
        Assert.Single(_h.Jira.Requests, r => FakeJira.Jql(r).StartsWith("key in")); // not one per day
    }

    [Fact]
    public async Task When_every_day_is_saved_there_is_nothing_to_fix()
    {
        var sheet = await SheetAsync();
        sheet.WorkFor(Monday).Drafts.Add(Row("ABC-1", 8));

        var result = await _h.Sync.SaveDaysAsync(_h.Connection, sheet);

        Assert.Null(result.FirstToFix);
        Assert.Null(result.Notice);
    }
}
