using System.Net;
using System.Text.Json.Nodes;
using JiraTime.Domain;
using static JiraTime.Tests.TestEntries;

namespace JiraTime.Tests;

public class MonthExportLoaderTests : IDisposable
{
    private static readonly DateOnly October = new(2026, 10, 1);
    private readonly TimeHarness _h = new(); // today is Tue 27 Oct 2026

    public MonthExportLoaderTests()
    {
        _h.Jira.Gets["/rest/api/2/field"] = _ => new JsonArray(
            new JsonObject { ["id"] = "customfield_10008", ["name"] = "Epic Link", ["schema"] = new JsonObject { ["custom"] = "com.pyxis.greenhopper.jira:gh-epic-link" } });
        // ABC-1 is in epic BAC-577, ABC-2 has no epic.
        _h.Jira.AddIssue("ABC-1", "Summary of ABC-1");
        _h.Jira.Issues["ABC-1"]["customfield_10008"] = "BAC-577";
        _h.Jira.AddIssue("ABC-2", "Summary of ABC-2");
    }

    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Rows_epics_and_their_values_come_from_Jira_and_the_store()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-05T09:00:00.000+0200", 6 * 3600);
        _h.Jira.AddWorklog("ABC-2", "2026-10-05T09:00:00.000+0200", 2 * 3600);
        var (loader, values) = _h.ExportLoading();
        await values.SaveAsync(new EpicExportValues { EpicKey = "BAC-577", Fase = "13" });

        var export = await loader.LoadAsync(October);

        Assert.Null(export.JiraError);
        Assert.Equal(31 + 1, export.Rows.Count); // every day, and two issues on the 5th
        Assert.Equal(TimeSpan.FromHours(8), MonthExport.Total(export.Rows));
        Assert.Equal("13", export.Values["bac-577"].Fase);
        Assert.Equal(["ABC-2"], export.NoEpic);
        Assert.Equal(0, export.NotInJira);
    }

    [Fact]
    public async Task Work_waiting_for_Jira_is_included_and_counted()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-05T09:00:00.000+0200", 3600);
        await _h.Queue.AddAsync(Waiting("ABC-2", 2, new DateOnly(2026, 10, 6)));
        var (loader, _) = _h.ExportLoading();

        var export = await loader.LoadAsync(October);

        Assert.Equal(1, export.NotInJira);
        Assert.Contains(export.Rows, r => r.IssueKey == "ABC-2" && r.Hours == TimeSpan.FromHours(2));
        Assert.Empty(export.Values["BAC-577"].Fase ?? "");
    }

    [Fact]
    public async Task Without_Jira_there_is_nothing_to_export()
    {
        _h.Jira.FailWith = HttpStatusCode.ServiceUnavailable;
        var (loader, _) = _h.ExportLoading();

        var export = await loader.LoadAsync(October);

        Assert.NotNull(export.JiraError);
        Assert.Empty(export.Rows);
    }

    [Fact]
    public async Task A_failed_epic_lookup_fails_the_load()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-05T09:00:00.000+0200", 3600);
        _h.Jira.FailWhen = uri => uri.AbsolutePath.EndsWith("/field") ? HttpStatusCode.InternalServerError : null;
        var (loader, _) = _h.ExportLoading();

        var export = await loader.LoadAsync(October);

        Assert.NotNull(export.JiraError);
        Assert.Empty(export.Rows);
    }
}
